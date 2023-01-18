using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TDbContext> : IOnceExecutor<TDbContext>, IOnceExecutor
    {
        protected abstract TDbContext Context { get; }

        public async Task<TResult?> Execute<TResult>(
            Guid idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteInTransaction(idempotentKey, async token =>
            {
                if (!await IsAlreadyExecuted(idempotentKey, token))
                {
                    await SaveIdempotentKey(idempotentKey, token);
                    await modificator(Context, token);
                    await OnModificationCompleted(token);
                }

                return selector != null
                    ? await selector(Context, token)
                    : default;
            }, cancellationToken);
        }

        public Task Execute(Guid idempotentKey, Func<TDbContext, CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return Execute<int>(idempotentKey, modificator, null, cancellationToken);
        }

        public Task<TResult?> Execute<TResult>(Guid idempotentKey,
            Func<CancellationToken, Task> modificator, Func<CancellationToken, Task<TResult?>> selector,
            CancellationToken cancellationToken = default)
        {
            return Execute<TResult>(idempotentKey, (_, token) => modificator(token), (_, token) => selector(token), cancellationToken: cancellationToken);
        }

        public Task Execute(Guid idempotentKey, Func<CancellationToken, Task> modificator, CancellationToken cancellationToken = default)
        {
            return Execute(idempotentKey, (_, token) => modificator(token), cancellationToken: cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransaction<TResult>(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken);

        protected abstract Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompleted(CancellationToken cancellationToken);
    }
}