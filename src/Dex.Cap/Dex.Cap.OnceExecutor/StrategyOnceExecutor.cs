using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor
{
    public abstract class StrategyOnceExecutor<TArg, TOptions, TResult, TExecutionStrategy> : IStrategyOnceExecutor<TArg, TResult>
        where TExecutionStrategy : class, IOnceExecutionStrategy<TArg, TOptions, TResult>
        where TOptions : IOnceExecutorOptions
    {
        protected TExecutionStrategy ExecutionStrategy { get; }

        protected StrategyOnceExecutor(TExecutionStrategy executionStrategy)
        {
            ExecutionStrategy = executionStrategy ?? throw new ArgumentNullException(nameof(executionStrategy));
        }

        public async Task<TResult?> ExecuteAsync(TArg argument, CancellationToken cancellationToken)
        {
            return await ExecuteInTransactionAsync(
                async token =>
                {
                    if (!await ExecutionStrategy.IsAlreadyExecutedAsync(argument, token).ConfigureAwait(false))
                    {
                        await ExecutionStrategy.ExecuteAsync(argument, cancellationToken).ConfigureAwait(false);
                        await OnExecuteCompletedAsync(cancellationToken).ConfigureAwait(false);
                    }

                    return await ExecutionStrategy.ReadAsync(argument, cancellationToken).ConfigureAwait(false);
                },
                async token => await ExecutionStrategy.IsAlreadyExecutedAsync(argument, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<TResult?> ExecuteInTransactionAsync(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            CancellationToken cancellationToken);

        protected abstract Task OnExecuteCompletedAsync(CancellationToken cancellationToken);
    }
}