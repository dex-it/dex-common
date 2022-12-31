using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<out TContext>
    {
        Task<TResult?> Execute<TResult>(Guid idempotentKey, Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult?>> selector, CancellationToken cancellationToken = default);

        Task Execute(Guid idempotentKey, Func<TContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default);
    }
}