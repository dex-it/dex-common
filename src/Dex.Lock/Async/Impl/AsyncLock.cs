using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Dex.Lock.Async.Impl
{
    public sealed class AsyncLock : IAsyncLock, IDisposable
    {
        [NotNull]
        private readonly SemaphoreSlim _semaphore;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Выполняет блокировку задачи (Task), все задачи запущенные через LockAsync будут выполнятся последовательно
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
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

        /// <summary>
        /// Action будет разделять блоировку совместно с задачами (Task) запущеными через LockAsync и будет выполнятся последовательно 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
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

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}