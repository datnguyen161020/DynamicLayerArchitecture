using System;

namespace DynamicLayerArchitecture.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryAttribute : Attribute
    {
        public string Query { get; set; }

        public QueryAttribute(string query)
        {
            Query = query;
        }
        
    }
}