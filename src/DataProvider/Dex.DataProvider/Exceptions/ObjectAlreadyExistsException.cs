using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public class ObjectAlreadyExistsException : DataProviderException
    {
        public ObjectAlreadyExistsException()
        {
        }

        public ObjectAlreadyExistsException(string message)
            : base(message)
        {
        }

        public ObjectAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ObjectAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}