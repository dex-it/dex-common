using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    internal sealed class StrategyOnceExecutorEf<TArg, TResult, TExecutionStrategy, TDbContext>
        : StrategyOnceExecutor<TArg, IEfTransactionOptions, TResult, TExecutionStrategy>
        where TDbContext : DbContext
        where TExecutionStrategy : class, IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>
    {
        private readonly TDbContext _dbContext;

        public StrategyOnceExecutorEf(TDbContext dbContext, TExecutionStrategy executionStrategy)
            : base(executionStrategy)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        protected override Task<TResult?> ExecuteInTransactionAsync(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            CancellationToken cancellationToken)
        {
            return _dbContext.ExecuteInTransactionScopeAsync(
                operation,
                verifySucceeded,
                ExecutionStrategy.Options,
                cancellationToken);
        }

        protected override Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
        {
            if (_dbContext.ChangeTracker.HasChanges())
                throw new UnsavedChangesDetectedException(_dbContext,
                    "Can't complete action, unsaved changes detected");

            return Task.CompletedTask;
        }
    }
}