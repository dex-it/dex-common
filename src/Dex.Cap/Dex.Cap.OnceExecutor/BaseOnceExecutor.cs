using System;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TContext, TResult> : IOnceExecutor<TContext, TResult>
    {
        protected abstract TContext Context { get; }

        public async Task<TResult> Execute(Guid stepId, Func<TContext, Task> modificator, Func<TContext, Task<TResult>> selector = null)
        {
            if (modificator == null) throw new ArgumentNullException(nameof(modificator));

            using var scope = BeginTransaction();
            if (!await IsAlreadyExecuted(stepId))
            {
                await SaveIdempotentKey(stepId);
                await modificator(Context);
                await OnModificationComplete();
            }

            var result = selector != null
                ? await selector(Context)
                : default;

            await CommitTransaction();
            return result;
        }

        protected abstract Task OnModificationComplete();

        protected abstract IDisposable BeginTransaction();
        protected abstract Task CommitTransaction();

        protected abstract Task<bool> IsAlreadyExecuted(Guid idempotentKey);
        protected abstract Task SaveIdempotentKey(Guid idempotentKey);
    }
}