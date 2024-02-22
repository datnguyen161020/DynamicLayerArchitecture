using System;

namespace DynamicLayerArchitecture.Exceptions
{
    public class SqlParameterException : Exception
    {
        public SqlParameterException(string message) : base(message)
        {
            
        }
        
    }
}