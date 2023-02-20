using System;
using System.Threading;
using System.Threading.Tasks;
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
            CancellationToken cancellationToken)
        {
            return await _dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionScopeAsync(
                operation, ExecutionStrategy.TransactionScopeOption, ExecutionStrategy.TransactionIsolationLevel, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}