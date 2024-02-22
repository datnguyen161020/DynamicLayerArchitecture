using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicLayerArchitecture.Exceptions;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace DynamicLayerArchitecture.Config
{
    public static class DatabaseDriverFactory
    {
        private const char ForwardSlash = '/';
        private const char BackSlash = '\\';
        private const char Comma = ',';

        private const string DriverFolderName = "packages";
        private const string DriverFileExtension = ".dll";
        
        private static readonly Dictionary<string, string> DriverDictionary = new Dictionary<string, string>
        {
            { "SqlClient", "SqlClient.SqlConnection" },
            { "MySqlConnector", "MySqlConnector.MySqlConnection" }
        };
        public static void InstallDriver()
        {
            var sqlDriverName = DynamicContainer.GetConfiguration<string>("SqlDriver");
            if (!DriverDictionary.ContainsKey(sqlDriverName))
            {
                throw new DriverInvalidException($"{sqlDriverName} not found or invalid name");
            }

            if (sqlDriverName.Equals("SqlClient"))
            {
                DynamicContainer.RegisterDriver(sqlDriverName,
                    () => new SqlConnection(DynamicContainer.GetConfiguration<string>("connectionString")));
                return;
            }
            
            var logger = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = repository.GetResourceAsync<PackageMetadataResource>(cancellationToken).Result;
            
            var metadata = resource
                .GetMetadataAsync(sqlDriverName, true, true, logger, CancellationToken.None)
                .Result;
            var packageSearchMetaData = metadata as IPackageSearchMetadata[] ?? metadata.ToArray();
            var versions = packageSearchMetaData.Select(m => m.Identity.Version)
                .OrderByDescending(v => v);
            var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                .SingleOrDefault() as TargetFrameworkAttribute;
            var nuGetFramework = NuGetFramework.ParseFrameworkName(targetFrameworkAttribute?.FrameworkName,
                new DefaultFrameworkNameProvider());

            NuGetVersion packageVersion = null;

            foreach (var version in versions)
            {
                var package = packageSearchMetaData.First(m => m.Identity.Version == version);
                var frameworks = package.DependencySets.Select(group => group.TargetFramework).ToList();
                if (!frameworks.Exists(framework => framework.Equals(nuGetFramework))) continue;
                packageVersion = new NuGetVersion(version);
                break;
            }
            
            CreateSource(sqlDriverName, packageVersion, nuGetFramework, sqlDriverName).Wait(cancellationToken);
        }
        
        private static async Task CreateSource(string packageId, NuGetVersion version, NuGetFramework nuGetFramework, string sqlDriverName)
        {
            var setting = Settings.LoadDefaultSettings(root: null);
            var packageSourceProvider = new PackageSourceProvider(setting);

            var sourceProvider = new SourceRepositoryProvider(packageSourceProvider, Repository.Provider.GetCoreV3());

            var packagePathResolver = new PackagePathResolver(Path.GetFullPath(DriverFolderName));
            var packageExtractionContext = new PackageExtractionContext(NullLogger.Instance);

            var repositories = sourceProvider.GetRepositories();
            var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

            await GetPackageDependencies(new PackageIdentity(packageId, version), 
                nuGetFramework, NullLogger.Instance, repositories.ToList(), availablePackages);

            var resolverContext = new PackageResolverContext(
                DependencyBehavior.Lowest,
                new[] { packageId },
                Enumerable.Empty<string>(),
                Enumerable.Empty<PackageReference>(), 
                Enumerable.Empty<PackageIdentity>(), 
                availablePackages, 
                sourceProvider.GetRepositories().Select(s => s.PackageSource),
                NullLogger.Instance);

            var resolver = new PackageResolver();
            var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                .Select(packageIdentity => availablePackages.Single(availablePackage => PackageIdentityComparer.Default.Equals(availablePackage, packageIdentity)));
            var frameworkReducer = new FrameworkReducer();

            var dlls = new List<string>();
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                return Assembly.Load(File.ReadAllBytes(dlls.Find(dllPath =>
                    dllPath.EndsWith(args.Name.Split(Comma)[0] + DriverFileExtension))));
            };
            
            foreach (var packageToInstall in packagesToInstall)
            {
                PackageReaderBase packageReader;
                var installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                if (installedPath == null)
                {
                    var downloadResource = await 
                        packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
                    
                    var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                        packageToInstall, 
                        new PackageDownloadContext(new SourceCacheContext()),
                        SettingsUtility.GetGlobalPackagesFolder(setting),
                        NullLogger.Instance, 
                        CancellationToken.None);
                    await PackageExtractor.ExtractPackageAsync(
                        downloadResult.PackageReader,
                        downloadResult.PackageStream,
                        packagePathResolver,
                        packageExtractionContext,
                        CancellationToken.None);
                    
                    packageReader = downloadResult.PackageReader;
                    installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                }
                else
                {
                    packageReader = new PackageFolderReader(installedPath);
                }
                
                var frameworkSpecificGroups = packageReader.GetLibItems().ToList();
                var nearest = frameworkReducer.GetNearest(
                    nuGetFramework, 
                    frameworkSpecificGroups.Select(frameworkSpecificGroup => frameworkSpecificGroup.TargetFramework));
                
                var dll = frameworkSpecificGroups
                    .Where(specificGroup => specificGroup.TargetFramework.Equals(nearest))
                    .SelectMany(specificGroup => specificGroup.Items)
                    .Where(name => name.EndsWith(DriverFileExtension));
                
                var dllPath = new StringBuilder(installedPath).Append(BackSlash.ToString()).Append(dll.FirstOrDefault())
                    .Replace(ForwardSlash.ToString(), BackSlash.ToString()).ToString();
                dlls.Add(dllPath);
                
                if (Path.GetFileName(dllPath).StartsWith(sqlDriverName))
                {
                    
                    var a = Assembly.Load(File.ReadAllBytes(dllPath));
                    var connectionType = a.GetType(DriverDictionary[sqlDriverName]);
                    
                    DynamicContainer.RegisterDriver(sqlDriverName,
                        () => Activator.CreateInstance(connectionType,
                            DynamicContainer.GetConfiguration<string>("connectionString")));
                }
                else
                {
                    Assembly.Load(File.ReadAllBytes(dllPath));
                }
            }
        }

        private static async Task GetPackageDependencies(PackageIdentity packageIdentity, 
            NuGetFramework framework, 
            ILogger logger,
            List<SourceRepository> repositories, 
            ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(packageIdentity)) return;

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(packageIdentity, framework, logger, CancellationToken.None);
                
                if (dependencyInfo == null) continue;

                availablePackages.Add(dependencyInfo);

                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    await GetPackageDependencies(new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        framework, logger, repositories, availablePackages);
                }
            }
            
        } 
    }
}