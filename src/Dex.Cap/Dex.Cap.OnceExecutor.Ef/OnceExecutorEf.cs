using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class OnceExecutorEf<TDbContext> : BaseOnceExecutor<IEfOptions, TDbContext>
        where TDbContext : DbContext
    {
        protected override TDbContext Context { get; }

        public OnceExecutorEf(TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation">Запрещено вызывать SaveChanges</param>
        /// <param name="verifySucceeded"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        protected override async Task<TResult?> ExecuteAndSaveInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            IEfOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            options ??= new EfOptions();

            return await Context.ExecuteAndSaveInTransactionAsync(
                    operation, verifySucceeded, options.TransactionScopeOption, options.IsolationLevel, options.TimeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override Task AddIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            Context.Add(new LastTransaction {IdempotentKey = idempotentKey});
            return Task.CompletedTask;
        }

        protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}