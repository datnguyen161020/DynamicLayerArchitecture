using System;
using System.Linq;
using DynamicLayerArchitecture.CustomAttributes;

namespace DynamicLayerArchitecture.Config
{
    public static class ComponentFactory
    {
        public static object CreateComponent(Type type)
        {
            if (DynamicContainer.IsExistComponent(type)) return DynamicContainer.Create(type);
            
            if (type.GetCustomAttributes(typeof(RepositoryAttribute), true).Length <= 0)
            {
                var constructors = type.GetConstructors();
                
                DynamicContainer.Register(type,
                    () => Activator.CreateInstance(type,
                        constructors[0].GetParameters()
                            .Select(constructor => CreateComponent(constructor.ParameterType)).ToArray()));
                return DynamicContainer.Create(type);
            }
            
            DynamicContainer.Register(type, () => GetRepositoryCreator(type));
            return DynamicContainer.Create(type);
        }

        private static object GetRepositoryCreator(Type type)
        {
            return DynamicRepository.CreateRepository(type);
        }
    }
}