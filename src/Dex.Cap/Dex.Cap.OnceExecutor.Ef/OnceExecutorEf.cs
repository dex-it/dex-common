using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
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

        protected override Task<TResult?> ExecuteInTransaction<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
            where TResult : default
        {
            return Context.Database.CreateExecutionStrategy()
                .ExecuteInTransactionScopeAsync(operation, transactionScopeOption, isolationLevel, cancellationToken);
        }

        protected override Task<bool> IsAlreadyExecuted(string idempotentKey, CancellationToken cancellationToken)
        {
            return Context.Set<LastTransaction>()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken);
        }

        protected override Task SaveIdempotentKey(string idempotentKey, CancellationToken cancellationToken)
        {
            return Context.AddAsync(new LastTransaction { IdempotentKey = idempotentKey }, cancellationToken).AsTask();
        }

        protected override Task OnModificationCompleted(CancellationToken cancellationToken)
        {
            return Context.SaveChangesAsync(cancellationToken);
        }
    }
}