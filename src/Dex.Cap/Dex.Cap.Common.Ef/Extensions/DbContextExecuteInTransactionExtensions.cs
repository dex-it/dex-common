using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Dex.Cap.Common.Ef.Extensions;

public static class DbContextExecuteInTransactionExtensions
{
    /// <summary>
    /// Executes the specified operation within a transaction using the DbContext's execution strategy.
    /// This method uses IDbContextTransaction (explicit transaction) instead of TransactionScope.
    /// Recommended for CQRS (Read/Write) and multi-database scenarios to avoid "Ambient transaction detected" errors.
    /// Supports nested calls (reentrancy) by participating in the existing transaction if one is already active.
    /// </summary>
    public static Task<TResult> ExecuteInTransactionAsync<TState, TResult>(
        this DbContext dbContext,
        TState state,
        Func<TState, CancellationToken, Task<TResult>> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        options ??= EfTransactionOptions.Default;

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return executionStrategy.ExecuteAsync(
            new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
            async (context, st, ct) =>
            {
                if (context.ChangeTracker.HasChanges())
                    throw new UnsavedChangesDetectedException(context, "Can't execute action, unsaved changes detected");

                // Set command timeout if specified
                var oldTimeout = context.Database.GetCommandTimeout();
                if (options.TimeoutInSeconds > 0)
                    context.Database.SetCommandTimeout((int)options.TimeoutInSeconds);

                var isNested = context.Database.CurrentTransaction != null;

                try
                {
                    if (isNested)
                    {
                        // Participate in existing transaction
                        st.Result = await st.Operation(st.State, ct).ConfigureAwait(false);

                        if (dbContext.ChangeTracker.HasChanges())
                        {
                            // Ensure changes are flushed before returning from nested call
                            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                        }

                        return st.Result;
                    }

                    var dataIsolationLevel = MapIsolationLevel(options.IsolationLevel);

                    await using var transaction = await context.Database.BeginTransactionAsync(dataIsolationLevel, ct).ConfigureAwait(false);

                    st.Result = await st.Operation(st.State, ct).ConfigureAwait(false);

                    if (context.ChangeTracker.HasChanges())
                    {
                        // In most cases, SaveChangesAsync should be called inside the operation delegate.
                        // However, we perform a final check to ensure all changes are flushed before commit.
                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    await transaction.CommitAsync(ct).ConfigureAwait(false);

                    return st.Result;
                }
                catch (Exception ex)
                    when (ExecutionStrategy.CallOnWrappedException(ex, exHandle => (exHandle as NpgsqlException)?.IsTransient == true || exHandle is TimeoutException))
                {
                    // If the strategy decides to retry, we may want to clear the change tracker to avoid conflicts
                    if (options.ClearChangeTrackerOnRetry)
                        context.ChangeTracker.Clear();

                    throw;
                }
                finally
                {
                    // Restore original timeout
                    if (options.TimeoutInSeconds > 0)
                        context.Database.SetCommandTimeout(oldTimeout);
                }
            },
            async (_, st, ct) => new ExecutionResult<TResult>(await st.VerifySucceeded(st.State, ct).ConfigureAwait(false), st.Result),
            cancellationToken
        );
    }

    /// <summary>
    /// Map System.Transactions.IsolationLevel to System.Data.IsolationLevel
    /// </summary>
    private static IsolationLevel MapIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
    {
        return isolationLevel switch
        {
            System.Transactions.IsolationLevel.Chaos => IsolationLevel.Chaos,
            System.Transactions.IsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
            System.Transactions.IsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
            System.Transactions.IsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
            System.Transactions.IsolationLevel.Serializable => IsolationLevel.Serializable,
            System.Transactions.IsolationLevel.Snapshot => IsolationLevel.Snapshot,
            System.Transactions.IsolationLevel.Unspecified => IsolationLevel.Unspecified,
            _ => IsolationLevel.ReadCommitted // Default to ReadCommitted if unknown
        };
    }

    #region Overloads

    /// <summary>
    /// Executes the specified operation within a transaction.
    /// Uses an explicit transaction (IDbContextTransaction).
    /// </summary>
    public static Task<TResult> ExecuteInTransactionAsync<TResult>(
        this DbContext dbContext,
        Func<CancellationToken, Task<TResult>> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionAsync<object, TResult>(
            null!,
            async (_, token) => await operation(token).ConfigureAwait(false),
            async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
            options,
            cancellationToken
        );

    /// <summary>
    /// Executes the specified action within a transaction.
    /// Uses an explicit transaction (IDbContextTransaction).
    /// </summary>
    public static Task ExecuteInTransactionAsync<TState>(
        this DbContext dbContext,
        TState state,
        Func<TState, CancellationToken, Task> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionAsync(
            state,
            async (st, ct) =>
            {
                await operation(st, ct).ConfigureAwait(false);
                return true;
            },
            verifySucceeded,
            options,
            cancellationToken
        );

    /// <summary>
    /// Executes the specified action within a transaction.
    /// Uses an explicit transaction (IDbContextTransaction).
    /// </summary>
    public static Task ExecuteInTransactionAsync(
        this DbContext dbContext,
        Func<CancellationToken, Task> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionAsync<object>(
            null!,
            async (_, token) => await operation(token).ConfigureAwait(false),
            async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
            options,
            cancellationToken
        );

    #endregion
}