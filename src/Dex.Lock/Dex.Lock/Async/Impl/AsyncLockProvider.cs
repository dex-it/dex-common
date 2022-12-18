using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dex.Lock.Async.Impl
{
    public sealed class AsyncLockProvider<T> : IAsyncLockProvider<T, LockReleaser> where T : notnull
    {
        private readonly ConcurrentDictionary<T, AsyncLock> _locks = new();

        public IAsyncLock<LockReleaser> GetLocker([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _locks.GetOrAdd(key, _ => new AsyncLock());
        }

        public Task<bool> RemoveLocker([NotNull] T key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Task.FromResult(_locks.TryRemove(key, out _));
        }
    }
}