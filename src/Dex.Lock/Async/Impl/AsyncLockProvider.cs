using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dex.Lock.Async.Impl
{
    public class AsyncLockProvider<T> : IAsyncLockProvider<T>
    {
        private readonly ConcurrentDictionary<T, AsyncLock> _locks = new ConcurrentDictionary<T, AsyncLock>();

        public IAsyncLock GetLock([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _locks.GetOrAdd(key, key1 => new AsyncLock());
        }

        public Task<bool> RemoveLock([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Task.FromResult(_locks.TryRemove(key, out _));
        }
    }
}