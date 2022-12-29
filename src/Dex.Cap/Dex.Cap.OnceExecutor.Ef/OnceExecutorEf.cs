using System;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async Task<TResult?> ExecuteInTransaction<TResult>(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken) where TResult : default
        {
            var strategy = Context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteInTransactionAsync(operation, c => IsAlreadyExecuted(idempotentKey, c), cancellationToken).ConfigureAwait(false);
        }

        protected override Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken)
        {
            return Context.AddAsync(new LastTransaction { IdempotentKey = idempotentKey }, cancellationToken).AsTask();
        }

        protected override Task OnModificationComplete()
        {
            return Context.SaveChangesAsync();
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>().AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken).ConfigureAwait(false);
        }
    }
}