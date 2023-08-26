using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.OnceExecutor.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class OnceExecutorEf<TDbContext> : BaseOnceExecutor<IEfOptions, TDbContext>
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
            IEfOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            options ??= new EfOptions();

            return await Context.ExecuteInTransactionScopeAsync(
                    operation, verifySucceeded, options.TransactionScopeOption, options.IsolationLevel, options.TimeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            return await Context.Set<LastTransaction>()
                .AnyAsync(x => x.IdempotentKey == idempotentKey, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            await Context.AddAsync(new LastTransaction { IdempotentKey = idempotentKey }, cancellationToken).AsTask().ConfigureAwait(false);
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