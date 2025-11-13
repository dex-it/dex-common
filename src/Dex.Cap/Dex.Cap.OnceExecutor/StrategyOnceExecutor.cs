using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.OnceExecutor;

public abstract class StrategyOnceExecutor<TArg, TOptions, TResult, TExecutionStrategy>(TExecutionStrategy executionStrategy)
    : IStrategyOnceExecutor<TArg, TResult>
    where TExecutionStrategy : class, IOnceExecutionStrategy<TArg, TOptions, TResult>
    where TOptions : ITransactionOptions
{
    protected TExecutionStrategy ExecutionStrategy { get; } = executionStrategy;

    public Task<TResult?> ExecuteAsync(TArg argument, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async token =>
            {
                if (!await ExecutionStrategy.IsAlreadyExecutedAsync(argument, token).ConfigureAwait(false))
                {
                    await ExecutionStrategy.ExecuteAsync(argument, token).ConfigureAwait(false);
                    await OnExecuteCompletedAsync(token).ConfigureAwait(false);
                }

                return await ExecutionStrategy.ReadAsync(argument, token).ConfigureAwait(false);
            },
            async token => await ExecutionStrategy.IsAlreadyExecutedAsync(argument, token).ConfigureAwait(false),
            cancellationToken);
    }

    protected abstract Task<TResult?> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<TResult?>> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        CancellationToken cancellationToken);

    protected abstract Task OnExecuteCompletedAsync(CancellationToken cancellationToken);
}