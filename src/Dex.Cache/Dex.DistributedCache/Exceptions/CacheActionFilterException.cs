using System;

namespace Dex.DistributedCache.Exceptions
{
    public class CacheActionFilterException : Exception
    {
        public CacheActionFilterException()
        {
        }

        public CacheActionFilterException(string message) : base(message)
        {
        }

        public CacheActionFilterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}