using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Dex.Cap.OnceExecutor
{
    public abstract class StrategyOnceExecutor<TArg, TExecutionStrategy, TResult> : IStrategyOnceExecutor<TArg, TResult>
        where TExecutionStrategy : IOnceExecutionStrategy<TArg, TResult>
    {
        protected TExecutionStrategy ExecutionStrategy { get; }

        protected StrategyOnceExecutor(TExecutionStrategy executionStrategy)
        {
            ExecutionStrategy = executionStrategy ?? throw new ArgumentNullException(nameof(executionStrategy));
        }

        public async Task<TResult?> ExecuteAsync(TArg argument, CancellationToken cancellationToken = default)
        {
            return await ExecuteInTransactionAsync(async token =>
            {
                if (!await ExecutionStrategy.CheckIdempotenceAsync(argument, token))
                {
                    await ExecutionStrategy.ExecuteAsync(argument, cancellationToken);
                    await OnExecuteCompletedAsync(cancellationToken);
                }

                return await ExecutionStrategy.ReadAsync(argument, cancellationToken);
            }, cancellationToken);
        }

        protected abstract Task<TResult?> ExecuteInTransactionAsync(Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellationToken);

        protected abstract Task OnExecuteCompletedAsync(CancellationToken cancellationToken);
    }

    public interface IStrategyOnceExecutor<in TArg, TResult>
    {
        Task<TResult?> ExecuteAsync(TArg arg, CancellationToken cancellationToken = default);
    }

    public interface IOnceExecutionStrategy<in TArg, TResult>
    {
        IsolationLevel TransactionIsolationLevel => IsolationLevel.ReadCommitted;

        Task<bool> CheckIdempotenceAsync(TArg argument, CancellationToken cancellationToken);

        Task ExecuteAsync(TArg argument, CancellationToken cancellationToken);

        Task<TResult?> ReadAsync(TArg argument, CancellationToken cancellationToken);
    }
}