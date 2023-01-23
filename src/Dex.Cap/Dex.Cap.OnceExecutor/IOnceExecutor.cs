using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<out TContext>
    {
        Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult?>> selector,
            CancellationToken cancellationToken = default);

        Task ExecuteAsync(
            string idempotentKey,
            Func<TContext, CancellationToken, Task> modificator,
            CancellationToken cancellationToken = default);
    }

    public interface IOnceExecutor
    {
        Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            CancellationToken cancellationToken = default);

        Task ExecuteAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            CancellationToken cancellationToken = default);
    }
}