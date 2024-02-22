using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DynamicLayerArchitecture.CustomAttributes;

namespace DynamicLayerArchitecture.Config
{
    [Component]
    public class DapperLogger
    {
        private readonly string _connectionName;

        public DapperLogger()
        {
            _connectionName = DynamicContainer.GetConfiguration<string>("SqlDriver");
        }

        public void LogAndExecute(Action<IDbConnection> action)
        {
            using (var connection = DynamicContainer.CreateDriver(_connectionName))
            {
                connection.Open();

                // Execute the action
                action.Invoke(connection);
            }
        }

        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            var customParams = param as CustomDynamicParameters;

            using (var connection = DynamicContainer.CreateDriver(_connectionName))
            {
                connection.Open();
                return connection.Query<T>(sql, customParams);
            }
        }

        public int Execute(string sql, object param = null)
        {
            var customParams = param as CustomDynamicParameters;
            using (var connection = DynamicContainer.CreateDriver(_connectionName))
            {
                connection.Open();
                return connection.Execute(sql, customParams);
            }
        }
    }
}