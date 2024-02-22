using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using DynamicLayerArchitecture.Exceptions;
using Newtonsoft.Json;

namespace DynamicLayerArchitecture.Config
{
    public class CustomDynamicParameters : SqlMapper.IDynamicParameters
    {
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        
        public void Add(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            _parameters.Add(name, value);
        }

        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            Console.WriteLine("Custom dynamic parameters");

            var sqlWithValue = ReplaceParametersInSql(command.CommandText);
            Console.WriteLine(sqlWithValue);
            command.CommandText = sqlWithValue;
        }
        
         private string ReplaceParametersInSql(string originalSql)
        {
            var query = new StringBuilder(originalSql);
            var queryParams = Array.FindAll(query.ToString().Split(' '), param => param.StartsWith(":"));

            foreach (var queryParam in queryParams)
            {
                var param = queryParam.Split('.');
                if (param.Length > 1)
                {
                    var parameterObject =
                        _parameters[param[0].Split(':').LastOrDefault() ?? throw new InvalidOperationException()];
                    var paramValue = parameterObject.GetType()
                        .GetProperty(param.LastOrDefault() ?? string.Empty)?.GetValue(parameterObject, null);
                    if (parameterObject.GetType().GetProperty(param.LastOrDefault() ?? string.Empty)?.PropertyType == typeof(string))
                    {
                        query.Replace(queryParam,
                            new StringBuilder(string.Empty).Append('\'')
                                .Append(JsonConvert.SerializeObject(paramValue)).Append('\'')
                                .Replace("\"", string.Empty).ToString());
                        continue;
                    }
                    query.Replace(queryParam, JsonConvert.SerializeObject(paramValue));
                    
                    continue;
                }

                var parameter = _parameters[queryParam.Split(':').LastOrDefault() ?? throw new InvalidOperationException()];
                if (parameter.GetType().IsPrimitive)
                {
                    query.Replace(queryParam, JsonConvert.SerializeObject(parameter));
                    continue;
                }

                if (parameter.GetType().IsGenericType || parameter.GetType().IsArray)
                {
                    query.Replace(queryParam, new StringBuilder(string.Empty).Append('(')
                        .Append(JsonConvert.SerializeObject(parameter)).Append(')').ToString());
                    continue;
                }

                ReplaceInsertData(query, parameter, queryParam);
            }
            
            return query.ToString();
        }

         private static void ReplaceInsertData(StringBuilder query, object parameter, string queryParam)
         {
             if (!query.ToString().StartsWith("Insert", StringComparison.InvariantCultureIgnoreCase))
             {
                 throw new SqlParameterException("Param not valid");
             }

             var insertData = new StringBuilder(string.Empty);
             foreach (var propertyInfo in parameter.GetType().GetProperties())
             {
                 if (insertData.Length != 0) insertData.Append(',');
                 if (propertyInfo.PropertyType.IsArray || propertyInfo.PropertyType.IsGenericType)
                 {
                     throw new SqlParameterException($"Cannot convert data type is {propertyInfo.PropertyType}");
                 }

                 var propertyValue = propertyInfo.GetValue(parameter, null);
                 if (propertyValue is null)
                 {
                     insertData.Append("null");
                     continue;
                 }
                 if (propertyInfo.PropertyType == typeof(string))
                 {
                     insertData.Append('\'')
                         .Append(propertyInfo.GetValue(parameter, null))
                         .Append('\'');
                     continue;
                 }
                 insertData.Append(propertyInfo.GetValue(parameter, null));
             }

             insertData.Insert(0, '(');
             insertData.Append(')');
             query.Replace(queryParam, insertData.ToString());
         }
    }
}