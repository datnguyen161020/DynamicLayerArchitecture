using System;

namespace DynamicLayerArchitecture.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; }
        
        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}