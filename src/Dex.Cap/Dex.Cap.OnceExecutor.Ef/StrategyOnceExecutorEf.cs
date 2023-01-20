using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.OnceExecutor.Ef.Helpers;
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

        protected override Task<TResult?> ExecuteInTransactionAsync(Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken)
        {
            return _dbContext.Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    using var transactionScope =
                        TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.Required, ExecutionStrategy.TransactionIsolationLevel);
                    var result = await operation(cancellationToken).ConfigureAwait(false);
                    transactionScope.Complete();
                    return result;
                });
        }

        protected override Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}