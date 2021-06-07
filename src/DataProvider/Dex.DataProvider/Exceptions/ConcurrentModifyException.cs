using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public sealed class ConcurrentModifyException : DataProviderException
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

        public ConcurrentModifyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}