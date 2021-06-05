using System;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public interface IOnceExecutor<out TContext, TResult>
    {
        Task<TResult?> Execute(Guid idempotentKey, Func<TContext, Task> modificator, Func<TContext, Task<TResult>>? selector = null);
    }
}