using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class StrategyOnceExecutorEf<TArg, TResult, TExecutionStrategy, TDbContext> : StrategyOnceExecutor<TArg, TResult, TExecutionStrategy>
        where TDbContext : DbContext
        where TExecutionStrategy : IOnceExecutionStrategy<TArg, TResult>
    {
        private readonly TDbContext _dbContext;

        public StrategyOnceExecutorEf(TDbContext dbContext, TExecutionStrategy executionStrategy) : base(executionStrategy)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            CancellationToken cancellationToken)
        {
            return await _dbContext.ExecuteInTransactionScopeAsync(
                    operation,
                    verifySucceeded,
                    ExecutionStrategy.TransactionScopeOption,
                    ExecutionStrategy.TransactionIsolationLevel,
                    ExecutionStrategy.TransactionTimeoutInSeconds,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        protected override Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
        {
            if (_dbContext.ChangeTracker.HasChanges())
                throw new UnsavedChangesDetectedException(_dbContext, "Can't complete action, unsaved changes detected");

            return Task.CompletedTask;
        }
    }
}