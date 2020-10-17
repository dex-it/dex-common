using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Dex.Lock.Async.Impl
{
    public class AsyncLock : IAsyncLock
    {
        [NotNull]
        private readonly SemaphoreSlim _semaphore;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        public async Task LockAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

            try
            {
                await _semaphore.WaitAsync();
                await asyncAction();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task LockAsync(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Task Act()
            {
                action();
                return Task.FromResult(true);
            }

            return LockAsync(Act);
        }
    }
}