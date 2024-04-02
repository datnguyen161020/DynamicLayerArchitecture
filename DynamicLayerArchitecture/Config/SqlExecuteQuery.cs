using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicLayerArchitecture.Config
{
    public static class SqlExecuteQuery
    {
        public static T Execute<T>(DapperLogger dapperLogger, Dictionary<string, object> param, string queryString)
        {
            object result = null;
            dapperLogger.LogAndExecute(connection =>
            {
                var customParams = new CustomDynamicParameters();
                foreach (var keyValuePair in param)
                {
                    customParams.Add(keyValuePair.Key, keyValuePair.Value);
                }

                if (!queryString.StartsWith("Select", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = dapperLogger.Execute(queryString, customParams);
                    return;
                }
                // handle select query
                if (!typeof(T).IsGenericType)
                {
                    // With return type is not list
                    result = dapperLogger.Query<T>(queryString, customParams).FirstOrDefault();
                    return;
                }
                var genericType = typeof(T).GetGenericArguments()[0];
                var resultGeneric = dapperLogger.Query<object>(queryString, customParams).ToList();

                if (!genericType.IsPrimitive && genericType != typeof(string))
                {
                    // generic type of list is object
                    result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(resultGeneric));
                    return;
                }
                    
                var values = new StringBuilder(string.Empty);
                foreach (var resultObject in resultGeneric)
                {
                    var jObject = JObject.Parse(JsonConvert.SerializeObject(resultObject));
                    var value = $"'{(jObject.First ?? throw new InvalidOperationException()).Values().FirstOrDefault()}'";
                    if (values.Length > 1) values.Append(',');
                    values.Append(value);
                }

                values.Insert(0, "[");
                values.Append("]");
                result = JsonConvert.DeserializeObject<T>(values.ToString());
            });
            return (T)result;
        }
    }
}