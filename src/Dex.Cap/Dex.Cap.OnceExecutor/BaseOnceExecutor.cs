using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MC.Core.Consistent.OnceExecutor
{
    public abstract class BaseOnceExecutor<TContext, TResult> : IOnceExecutor<TContext, TResult>
    {
        protected abstract TContext Context { get; }

        public async Task<TResult> Execute(Guid stepId, Func<TContext, Task> modificator, Func<TContext, Task<TResult>> selector = null)
        {
            if (modificator == null) throw new ArgumentNullException(nameof(modificator));

            using var scope = BeginTransaction();
            if (!await ExistStepId(stepId))
            {
                await SaveStepId(stepId);
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

        protected abstract Task<bool> ExistStepId(Guid stepId);
        protected abstract Task SaveStepId(Guid stepId);
    }
}