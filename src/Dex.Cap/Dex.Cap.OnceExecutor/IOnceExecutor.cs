using System;
using System.Threading.Tasks;

namespace MC.Core.Consistent.OnceExecutor
{
    public interface IOnceExecutor<out TContext, TResult>
    {
        Task<TResult> Execute(Guid stepId, Func<TContext, Task> modificator, Func<TContext, Task<TResult>> selector = null);
    }
}