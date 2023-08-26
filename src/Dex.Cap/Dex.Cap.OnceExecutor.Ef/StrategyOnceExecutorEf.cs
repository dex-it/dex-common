using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class StrategyOnceExecutorEf<TArg, TResult, TExecutionStrategy, TDbContext>
        : StrategyOnceExecutor<TArg, IEfOptions, TResult, TExecutionStrategy>
        where TDbContext : DbContext
        where TExecutionStrategy : class, IOnceExecutionStrategy<TArg, IEfOptions, TResult>
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
            ExecutionStrategy.Options ??= new EfOptions();

            return await _dbContext.ExecuteInTransactionScopeAsync(
                    operation,
                    verifySucceeded,
                    ExecutionStrategy.Options.TransactionScopeOption,
                    ExecutionStrategy.Options.IsolationLevel,
                    ExecutionStrategy.Options.TimeoutInSeconds,
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