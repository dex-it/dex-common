using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TDbContext> : IOnceExecutor<TDbContext>, IOnceExecutor
    {
        protected abstract TDbContext Context { get; }

        public async Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            return await ExecuteInTransaction(async token =>
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
            }, transactionScopeOption, isolationLevel, cancellationToken).ConfigureAwait(false);
        }

        public Task ExecuteAsync(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync<int>(idempotentKey, modificator, null, transactionScopeOption, isolationLevel, cancellationToken);
        }

        public Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(idempotentKey, (_, token) => modificator(token), (_, token) => selector(token),
                transactionScopeOption, isolationLevel, cancellationToken);
        }

        public Task ExecuteAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(idempotentKey, (_, token) => modificator(token), transactionScopeOption, isolationLevel, cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransaction<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecuted(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task SaveIdempotentKey(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompleted(CancellationToken cancellationToken);
    }
}