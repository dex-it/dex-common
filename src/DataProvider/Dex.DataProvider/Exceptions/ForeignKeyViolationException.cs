using System;
using System.Runtime.Serialization;

namespace Dex.DataProvider.Exceptions
{
    public class ForeignKeyViolationException : DataProviderException
    {
        public ForeignKeyViolationException()
        {
        }

        public ForeignKeyViolationException(string message)
            : base(message)
        {
        }

        public ForeignKeyViolationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ForeignKeyViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}