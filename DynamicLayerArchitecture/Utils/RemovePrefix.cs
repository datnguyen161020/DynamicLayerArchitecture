using System.Linq;
using System.Text.RegularExpressions;

namespace DynamicLayerArchitecture.Utils
{
    public static class RemovePrefix
    {
        public static string RemoveControllerPrefixFromNameClass(string className)
        {
            var tokens = Regex.Split(className, Constant.ControllerPrefix, RegexOptions.IgnoreCase);
            return tokens.Length == 0 ? className : tokens.FirstOrDefault();
        }
    }
}