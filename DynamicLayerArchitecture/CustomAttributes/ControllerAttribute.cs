using DynamicLayerArchitecture.Utils;

namespace DynamicLayerArchitecture.CustomAttributes
{
    public class ControllerAttribute : ComponentAttribute
    {
        public ControllerAttribute(string name)
        {
            Name = RemovePrefix.RemoveControllerPrefixFromNameClass(name);
        }
    }
}