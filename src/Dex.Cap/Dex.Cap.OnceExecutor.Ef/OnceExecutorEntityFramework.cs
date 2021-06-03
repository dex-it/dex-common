using System;
using System.Threading.Tasks;
using System.Transactions;
using MC.Core.Consistent.OnceExecutor;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public class OnceExecutorEntityFramework<TDbContext, T> : BaseOnceExecutor<TDbContext, T>, IOnceExecutorEntityFramework<TDbContext, T> where TDbContext : DbContext
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

        protected override async Task<bool> ExistStepId(Guid stepId)
        {
            return await Context.FindAsync<LastTransaction>(stepId) != null;
        }

        protected override Task SaveStepId(Guid stepId)
        {
            return Context.AddAsync(new LastTransaction {Last = stepId}).AsTask();
        }
    }
}