using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class OnceExecutorEf<TDbContext> : BaseOnceExecutor<IEfOptions, TDbContext>
        where TDbContext : DbContext
    {
        protected override TDbContext Context { get; }
        private LastTransaction? LastTransaction { get; set; }

        public OnceExecutorEf(TDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            IEfOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            options ??= new EfOptions();

            return await Context.ExecuteInTransactionScopeAsync(
                    operation, verifySucceeded, options.TransactionScopeOption, options.IsolationLevel,
                    options.TimeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey,
            CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>()
                .AsNoTracking()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
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

            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            if (Context.ChangeTracker.HasChanges())
                throw new UnsavedChangesDetectedException(Context, "Can't complete action, unsaved changes detected");

            return Task.CompletedTask;
        }
    }
}