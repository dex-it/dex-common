using System;
using System.IO;
using Dex.Lock.Async;

namespace Dex.Lock
{
    public abstract class BaseLockProvider<T> : IAsyncLockProvider<string, T> where T : IDisposable
    {
        public abstract string InstanceKey { get; }

        public abstract IAsyncLock<T> GetLocker(string key);

        protected string CreateKey(string key)
        {
            var nKey = key.RemoveSymbols();

            if (string.IsNullOrEmpty(nKey))
                throw new InvalidDataException("key, must contains only letters, digits (en locale)");

            return InstanceKey + nKey;
        }
    }
}