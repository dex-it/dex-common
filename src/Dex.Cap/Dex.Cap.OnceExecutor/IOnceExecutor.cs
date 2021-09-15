using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<out TContext, TResult>
    {
        Task<TResult?> Execute(Guid idempotentKey, 
            Func<TContext, CancellationToken, Task> modificator, 
            Func<TContext, CancellationToken, Task<TResult>>? selector = null,
            CancellationToken cancellationToken = default);
    }
}