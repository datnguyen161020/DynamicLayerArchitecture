using System;

namespace DynamicLayerArchitecture.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ComponentAttribute : Attribute
    {
        public string Name { get; set; }

        public ComponentAttribute(string name = "")
        {
            Name = name;
        }
    }
}