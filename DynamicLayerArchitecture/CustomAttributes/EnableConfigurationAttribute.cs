using System;
using DynamicLayerArchitecture.Config;

namespace DynamicLayerArchitecture.CustomAttributes
{
    public class EnableConfigurationAttribute : Attribute
    {
        public EnableConfigurationAttribute()
        {
            DatabaseDriverFactory.InstallDriver();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(ConfigurationAttribute), true).Length <= 0) continue;

                    var configuration = Activator.CreateInstance(type);
                    foreach (var methodInfo in type.GetMethods())
                    {
                        var param = methodInfo.GetParameters();
                        methodInfo.Invoke(configuration, param.Length == 0 ? null : new[] { configuration });
                    }
                }
            }
        }
    }
}