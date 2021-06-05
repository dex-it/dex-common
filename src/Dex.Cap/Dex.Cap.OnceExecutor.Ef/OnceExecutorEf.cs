using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dex.Cap.OnceExecutor.Ef
{
    public class OnceExecutorEf<TDbContext, TResult> : BaseOnceExecutor<TDbContext, TResult>, IOnceExecutorEf<TDbContext, TResult>
        where TDbContext : DbContext
    {
        private static readonly EmptyDisposable Empty = new();
        private IDbContextTransaction? _current;

        protected override TDbContext Context { get; }

        public OnceExecutorEf(TDbContext context)
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
            return Empty;
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