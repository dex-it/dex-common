using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public sealed class AccessModifyException : DataProviderException
    {
        public AccessModifyException()
        {
        }

        public AccessModifyException(string message) 
            : base(message)
        {
        }

        public AccessModifyException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public AccessModifyException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}