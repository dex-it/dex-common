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

        protected override async Task<TResult?> ExecuteInTransaction<TResult>(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation, CancellationToken cancellationToken) where TResult : default
        {
            return await Context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(operation, 
                token => IsAlreadyExecuted(idempotentKey, token),
                cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>().AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken)
        {
            await Context.AddAsync(new LastTransaction { IdempotentKey = idempotentKey }, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnModificationCompleted(CancellationToken cancellationToken)
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}