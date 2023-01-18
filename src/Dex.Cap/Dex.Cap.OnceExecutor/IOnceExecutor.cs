using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<out TContext>
    {
        Task<TResult?> Execute<TResult>(string idempotentKey, Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult?>> selector, CancellationToken cancellationToken = default);

        Task Execute(string idempotentKey, Func<TContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default);
    }

    public interface IOnceExecutor
    {
        Task<TResult?> Execute<TResult>(string idempotentKey, Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector, CancellationToken cancellationToken = default);

        Task Execute(string idempotentKey, Func<CancellationToken, Task> modificator, CancellationToken cancellationToken = default);
    }
}