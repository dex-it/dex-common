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
            uint timeoutInSeconds,
            CancellationToken cancellationToken)
        {
            return await ExecuteInTransactionAsync(
                async token =>
                {
                    if (!await IsAlreadyExecutedAsync(idempotentKey, token).ConfigureAwait(false))
                    {
                        await modificator(Context, token).ConfigureAwait(false);
                        await SaveIdempotentKeyAsync(idempotentKey, token).ConfigureAwait(false);
                        await OnModificationCompletedAsync(token).ConfigureAwait(false);
                    }

                    return selector != null
                        ? await selector(Context, token).ConfigureAwait(false)
                        : default;
                },
                async token => await IsAlreadyExecutedAsync(idempotentKey, token).ConfigureAwait(false),
                transactionScopeOption, isolationLevel, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            uint timeoutInSeconds,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync<int>(idempotentKey, modificator, null, transactionScopeOption, isolationLevel, timeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            uint timeoutInSeconds,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                async (_, token) => await selector(token).ConfigureAwait(false),
                transactionScopeOption, isolationLevel, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            uint timeoutInSeconds,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                transactionScopeOption, isolationLevel, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            uint timeoutInSeconds,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompletedAsync(CancellationToken cancellationToken);
    }
}