using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public class ConcurrentModifyException : DataProviderException
    {
        public ConcurrentModifyException()
        {
        }

        public ConcurrentModifyException(string message)
            : base(message)
        {
        }

        public ConcurrentModifyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ConcurrentModifyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}