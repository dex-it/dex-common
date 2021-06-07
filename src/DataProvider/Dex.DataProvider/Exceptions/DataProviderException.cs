using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public class DataProviderException : Exception
    {
        public DataProviderException()
        {
        }

        public DataProviderException(string message) 
            : base(message)
        {
        }

        public DataProviderException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
        
        protected DataProviderException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}