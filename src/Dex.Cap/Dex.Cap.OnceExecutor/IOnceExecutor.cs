using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<in TOptions, out TContext>
        where TOptions : IOnceExecutorOptions
    {
        Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult?>> selector,
            TOptions? options = default,
            CancellationToken cancellationToken = default);

        Task ExecuteAsync(
            string idempotentKey,
            Func<TContext, CancellationToken, Task> modificator,
            TOptions? options = default,
            CancellationToken cancellationToken = default);
    }

    public interface IOnceExecutor<in TOptions>
        where TOptions : IOnceExecutorOptions
    {
        Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            TOptions? options = default,
            CancellationToken cancellationToken = default);

        Task ExecuteAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            TOptions? options = default,
            CancellationToken cancellationToken = default);
    }
}