using System;
using System.Collections.Generic;
using System.Data;

namespace DynamicLayerArchitecture.Config
{
    public static class DynamicContainer
    {
        public delegate object Creator();

        private static readonly Dictionary<string, Creator> DatabaseDriverToCreators = new Dictionary<string, Creator>();

        private static readonly Dictionary<Type, Creator> TypeToCreator = new Dictionary<Type, Creator>();

        public static Dictionary<string, object> Configuration { get; } = new Dictionary<string, object>();

        public static void Register(Type type, Creator creator)
        {
            TypeToCreator.Add(type, creator);
        }

        public static object Create(Type type)
        {
            return TypeToCreator[type]();
        }

        public static void RegisterDriver(string driverName, Creator creator)
        {
            DatabaseDriverToCreators.Add(driverName, creator);
        }
        
        public static IDbConnection CreateDriver(string driverName)
        {
            return DatabaseDriverToCreators[driverName]() as IDbConnection;
        }

        public static T GetConfiguration<T>(string name)
        {
            return (T) Configuration[name];
        }

        public static bool IsExistComponent(Type type)
        {
            return TypeToCreator.ContainsKey(type);
        }
    }
    
}