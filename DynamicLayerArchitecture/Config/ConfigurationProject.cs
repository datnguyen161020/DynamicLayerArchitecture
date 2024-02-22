using System;
using DynamicLayerArchitecture.CustomAttributes;

namespace DynamicLayerArchitecture.Config
{
    [Configuration]
    public class ConfigurationProject
    {
        public void ComponentConfiguration()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(ComponentAttribute), true).Length <= 0) continue;
                    ComponentFactory.CreateComponent(type);
                }
            }
        }

       
    }
}