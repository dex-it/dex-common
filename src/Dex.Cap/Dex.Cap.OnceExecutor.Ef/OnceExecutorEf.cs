using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dex.Cap.OnceExecutor.Ef
{
    internal sealed class OnceExecutorEf<TDbContext> : BaseOnceExecutor<IEfTransactionOptions, TDbContext>
        where TDbContext : DbContext
    {
        protected override TDbContext Context { get; }
        private LastTransaction? LastTransaction { get; set; }

        public OnceExecutorEf(TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected override Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            IEfTransactionOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            return Context.ExecuteInTransactionScopeAsync(operation, verifySucceeded, options, cancellationToken);
        }

        protected override Task<bool> IsAlreadyExecutedAsync(string idempotentKey,
            CancellationToken cancellationToken)
        {
            return Context.Set<LastTransaction>()
                .AsNoTracking()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken);
        }

        protected override Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            // в случае использования ретрай стратегии EnableRetryOnFailure метод вызовется несколько раз
            // и если LastTransaction уже был добавлен в DbContext то меняем его State на Added
            // поиск в ChangeTracker осуществляем по инстансу

            EntityEntry? existingEntry = null;

            if (LastTransaction != null)
            {
                existingEntry = Context.ChangeTracker
                    .Entries<LastTransaction>()
                    .FirstOrDefault(t => t.Entity == LastTransaction);
            }

            if (existingEntry == null)
            {
                LastTransaction = new LastTransaction { IdempotentKey = idempotentKey };
                Context.Add(LastTransaction);
            }
            else
            {
                existingEntry.State = EntityState.Added;
            }

            return Context.SaveChangesAsync(cancellationToken);
        }

        protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            if (Context.ChangeTracker.HasChanges())
                throw new UnsavedChangesDetectedException(Context, "Can't complete action, unsaved changes detected");

            return Task.CompletedTask;
        }
    }
}