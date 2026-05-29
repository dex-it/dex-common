using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.OnceExecutor.Ef;

internal sealed class StrategyOnceExecutorEf<TArg, TResult, TExecutionStrategy, TDbContext>(TDbContext dbContext, TExecutionStrategy executionStrategy)
    : StrategyOnceExecutor<TArg, IEfTransactionOptions, TResult, TExecutionStrategy>(executionStrategy)
    where TDbContext : DbContext
    where TExecutionStrategy : class, IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>
{
    protected override Task<TResult?> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<TResult?>> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        CancellationToken cancellationToken)
    {
        return dbContext.ExecuteInTransactionAsync(
            operation,
            verifySucceeded,
            ExecutionStrategy.Options,
            cancellationToken);
    }

    protected override Task OnExecuteCompletedAsync(CancellationToken cancellationToken)
    {
        return dbContext.ChangeTracker.HasChanges()
            ? throw new UnsavedChangesDetectedException(dbContext, "Can't complete action, unsaved changes detected")
            : Task.CompletedTask;
    }
}