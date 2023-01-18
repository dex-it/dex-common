using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef
{
    public sealed class StrategyOnceExecutorEf<TArg, TDbContext, TExecutionStrategy, TResult> : StrategyOnceExecutor<TArg, TExecutionStrategy, TResult>
        where TDbContext : DbContext
        where TExecutionStrategy : IOnceExecutionStrategy<TArg, TResult>
    {
        private readonly TDbContext _dbContext;

        public StrategyOnceExecutorEf(TDbContext dbContext, TExecutionStrategy executionStrategy) : base(executionStrategy)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync(Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    using var transactionScope =
                        TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.Required, ExecutionStrategy.TransactionIsolationLevel);
                    var result = await operation(cancellationToken).ConfigureAwait(false);
                    transactionScope.Complete();
                    return result;
                }).ConfigureAwait(false);
        }

        protected override async Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}