using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class BaseOnceExecutor<TOptions, TDbContext> : IOnceExecutor<TOptions, TDbContext>, IOnceExecutor<TOptions>
        where TOptions : IOnceExecutorOptions
    {
        protected abstract TDbContext Context { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idempotentKey"></param>
        /// <param name="modificator">Запрещено вызывать SaveChanges</param>
        /// <param name="selector"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public async Task<TResult?> ExecuteAndSaveInTransactionAsync<TResult>(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            Func<TDbContext, CancellationToken, Task<TResult?>>? selector,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            return await ExecuteAndSaveInTransactionAsync(
                async token =>
                {
                    if (!await IsAlreadyExecutedAsync(idempotentKey, token).ConfigureAwait(false))
                    {
                        await AddIdempotentKeyAsync(idempotentKey, token).ConfigureAwait(false);
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

        public async Task ExecuteAndSaveInTransactionAsync(
            string idempotentKey,
            Func<TDbContext, CancellationToken, Task> modificator,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            await ExecuteAndSaveInTransactionAsync<int>(idempotentKey, modificator, null, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResult?> ExecuteAndSaveInTransactionAsync<TResult>(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            Func<CancellationToken, Task<TResult?>> selector,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            return await ExecuteAndSaveInTransactionAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                async (_, token) => await selector(token).ConfigureAwait(false),
                options,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task ExecuteAndSaveInTransactionAsync(
            string idempotentKey,
            Func<CancellationToken, Task> modificator,
            TOptions? options,
            CancellationToken cancellationToken)
        {
            await ExecuteAndSaveInTransactionAsync(
                idempotentKey,
                async (_, token) => await modificator(token).ConfigureAwait(false),
                options,
                cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<TResult?> ExecuteAndSaveInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TOptions? options,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task AddIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken);

        protected abstract Task OnModificationCompletedAsync(CancellationToken cancellationToken);
    }
}