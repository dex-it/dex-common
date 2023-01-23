using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TDbContext> : IOnceExecutor<TDbContext>, IOnceExecutor
    {
        protected abstract TDbContext Context { get; }

        public async Task<TResult?> ExecuteAsync<TResult>(string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator, Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteInTransaction(idempotentKey, async token =>
            {
                if (!await IsAlreadyExecuted(idempotentKey, token).ConfigureAwait(false))
                {
                    await SaveIdempotentKey(idempotentKey, token).ConfigureAwait(false);
                    await modificator(Context, token).ConfigureAwait(false);
                    await OnModificationCompleted(token).ConfigureAwait(false);
                }

                return selector != null
                    ? await selector(Context, token).ConfigureAwait(false)
                    : default;
            }, cancellationToken).ConfigureAwait(false);
        }

        public Task ExecuteAsync(string idempotentKey, Func<TDbContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<int>(idempotentKey, modificator, null, cancellationToken);
        }

        public Task<TResult?> ExecuteAsync<TResult>(string idempotentKey,
            Func<CancellationToken, Task> modificator, Func<CancellationToken, Task<TResult?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(idempotentKey, (_, token) => modificator(token), (_, token) => selector(token), cancellationToken: cancellationToken);
        }

        public Task ExecuteAsync(string idempotentKey, Func<CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(idempotentKey, (_, token) => modificator(token), cancellationToken: cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransaction<TResult>(string idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecuted(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task SaveIdempotentKey(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompleted(CancellationToken cancellationToken);
    }
}