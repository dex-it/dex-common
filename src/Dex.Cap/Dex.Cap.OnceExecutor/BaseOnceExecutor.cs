using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TOptions, TDbContext> : IOnceExecutor<TOptions, TDbContext>, IOnceExecutor<TOptions>
        where TOptions : IOnceExecutorOptions
    {
        protected abstract TDbContext Context { get; }

        public async Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            return await ExecuteInTransactionAsync(
                async token =>
                {
                    if (!await IsAlreadyExecutedAsync(idempotentKey, token).ConfigureAwait(false))
                    {
                        await SaveIdempotentKeyAsync(idempotentKey, token).ConfigureAwait(false);
                        await modificator(Context, token).ConfigureAwait(false);
                        await OnModificationCompletedAsync(token).ConfigureAwait(false);
                    }

                    return selector != null
                        ? await selector(Context, token).ConfigureAwait(false)
                        : default;
                },
                async token => await IsAlreadyExecutedAsync(idempotentKey, token).ConfigureAwait(false),
                options, cancellationToken).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync<int>(idempotentKey, modificator, null, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResult?> ExecuteAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                async (_, token) => await selector(token).ConfigureAwait(false),
                options,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                options,
                cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TOptions? options,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompletedAsync(CancellationToken cancellationToken);
    }
}