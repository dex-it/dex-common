using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class OnceExecutorEf<TDbContext> : BaseOnceExecutor<TDbContext>
        where TDbContext : DbContext
    {
        protected override TDbContext Context { get; }

        public OnceExecutorEf(TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
            where TResult : default
        {
            return await Context.ExecuteInTransactionScopeAsync(operation, verifySucceeded, transactionScopeOption, isolationLevel, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            await Context.AddAsync(new LastTransaction { IdempotentKey = idempotentKey }, cancellationToken).AsTask().ConfigureAwait(false);
        }

        protected override async Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}