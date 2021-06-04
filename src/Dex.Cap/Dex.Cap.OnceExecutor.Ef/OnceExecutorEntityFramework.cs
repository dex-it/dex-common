using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public class OnceExecutorEntityFramework<TDbContext, TResult> : BaseOnceExecutor<TDbContext, TResult>, IOnceExecutorEntityFramework<TDbContext, TResult>
        where TDbContext : DbContext
    {
        private TransactionScope _current;
        protected override TDbContext Context { get; }

        public OnceExecutorEntityFramework(TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }


        protected override Task OnModificationComplete()
        {
            return Context.SaveChangesAsync();
        }

        protected override IDisposable BeginTransaction()
        {
            return _current = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
        }

        protected override Task CommitTransaction()
        {
            _current.Complete();
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey)
        {
            return await Context.FindAsync<LastTransaction>(idempotentKey) != null;
        }

        protected override Task SaveIdempotentKey(Guid idempotentKey)
        {
            return Context.AddAsync(new LastTransaction {IdempotentKey = idempotentKey}).AsTask();
        }
    }
}