using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TContext, TResult> : IOnceExecutor<TContext, TResult>
    {
        protected abstract TContext Context { get; }

        public Task<TResult?> Execute(Guid idempotentKey,
            Func<TContext, CancellationToken, Task> modificator,
            Func<TContext, CancellationToken, Task<TResult>>? selector = null,
            CancellationToken cancellationToken = default)
        {
            if (modificator == null) throw new ArgumentNullException(nameof(modificator));

            return ExecuteInTransaction(idempotentKey, async (cancellationToken) =>
            {
                if (!await IsAlreadyExecuted(idempotentKey, cancellationToken))
                {
                    await SaveIdempotentKey(idempotentKey, cancellationToken);
                    await modificator(Context, cancellationToken);
                    await OnModificationComplete();
                }

                var result = selector != null
                    ? await selector(Context, cancellationToken)
                    : default;

                return result;
            }, cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransaction(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation, CancellationToken cancellationToken);
        protected abstract Task OnModificationComplete();
        protected abstract Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken);
        protected abstract Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken);
    }
}