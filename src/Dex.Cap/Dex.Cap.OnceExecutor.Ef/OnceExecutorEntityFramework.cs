using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dex.Cap.OnceExecutor.Ef
{
    public class OnceExecutorEntityFramework<TDbContext, TResult> : BaseOnceExecutor<TDbContext, TResult>, IOnceExecutorEntityFramework<TDbContext, TResult>
        where TDbContext : DbContext
    {
        private IDbContextTransaction _current;
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
            if (Context.Database.CurrentTransaction == null)
                return _current = Context.Database.BeginTransaction();
            return new EmptyDisposable();
        }

        protected override Task CommitTransaction()
        {
            return _current?.CommitAsync() ?? Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey)
        {
            return await Context.FindAsync<LastTransaction>(idempotentKey) != null;
        }

        protected override Task SaveIdempotentKey(Guid idempotentKey)
        {
            return Context.AddAsync(new LastTransaction {IdempotentKey = idempotentKey}).AsTask();
        }

        private class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}