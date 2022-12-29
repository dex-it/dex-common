using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TContext, TResult> : IOnceExecutor<TContext, TResult>
    {
        protected abstract TContext Context { get; }

        public async Task<TResult?> Execute(Guid idempotentKey,
            Func<TContext, CancellationToken, Task> modificator, Func<TContext, CancellationToken, Task<TResult?>>? selector,
            CancellationToken cancellationToken = default)
        {
            var result = await ExecuteInTransaction(idempotentKey, async (token) =>
            {
                if (!await IsAlreadyExecuted(idempotentKey, token))
                {
                    await SaveIdempotentKey(idempotentKey, token);
                    await modificator(Context, token);
                    await OnModificationComplete();
                }

                var result = selector != null
                    ? await selector(Context, token)
                    : default;

                return result;
            }, cancellationToken);

            return result;
        }

        public Task Execute(Guid idempotentKey, Func<TContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return Execute(idempotentKey, modificator, null, cancellationToken);
        }

        // impl

        protected abstract Task<TResult?> ExecuteInTransaction(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken);

        protected abstract Task OnModificationComplete();
        protected abstract Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken);
        protected abstract Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken);
    }
}