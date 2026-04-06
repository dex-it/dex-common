using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Dex.Cap.Common.Ef.Extensions;

public static class DbContextExecuteInTransactionExtensions
{
    /// <summary>
    /// Executes the specified operation within a transaction using the DbContext's execution strategy.
    /// This method uses IDbContextTransaction (explicit transaction) instead of TransactionScope.
    /// Recommended for CQRS (Read/Write) and multi-database scenarios to avoid "Ambient transaction detected" errors and DTC escalation.
    /// Supports nested calls (reentrancy) by participating in the existing transaction using Savepoints for nested atomicity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For nested calls within the same <see cref="DbContext"/> instance, a Savepoint is created to allow partial rollback 
    /// of only the nested operation. Note that a failed nested operation (savepoint rollback) does NOT automatically 
    /// roll back the outer transaction, allowing the caller to catch the exception and continue.
    /// </para>
    /// <para>
    /// If an existing transaction is present, its isolation level is checked against the requested level. 
    /// If the requested level is stricter than the current one, an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// <para>
    /// <b>Critical:</b> This method does NOT support atomicity across different <see cref="DbContext"/> instances. 
    /// If a nested call uses a different context instance, an <see cref="InvalidOperationException"/> is thrown 
    /// to prevent silent atomicity loss.
    /// </para>
    /// <para>
    /// <b>Retry Strategy:</b> The outer <see cref="IExecutionStrategy"/> only manages the primary <see cref="DbContext"/>. 
    /// If other contexts are used within the operation delegate, ensure their transient failures are handled 
    /// appropriately (e.g., by bubbling up to the outer strategy) to avoid conflicting or redundant retry logic.
    /// </para>
    /// <para>
    /// <b>Warning:</b> <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> is automatically called before committing 
    /// or returning from a nested call if changes are detected in the <see cref="ChangeTracker"/>. 
    /// However, failed entities are NOT automatically detached from the tracker after a savepoint rollback. 
    /// You MUST manually detach them or clear the tracker if you intend to continue the outer transaction, 
    /// otherwise subsequent <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> calls will fail.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await context.ExecuteInTransactionAsync(async ct => {
    ///     try {
    ///         await context.ExecuteInTransactionAsync(async ctInner => {
    ///             context.Users.Add(new User { Name = "Bad" });
    ///             await context.SaveChangesAsync(ctInner);
    ///             throw new Exception("Nested fail");
    ///         }, ct);
    ///     } catch (Exception) {
    ///         // IMPORTANT: Manual detach required!
    ///         var entry = context.ChangeTracker.Entries().FirstOrDefault(x => x.State == EntityState.Added);
    ///         if (entry != null) entry.State = EntityState.Detached;
    ///     }
    ///     return true;
    /// });
    /// </code>
    /// </example>
    public static Task<TResult> ExecuteInTransactionAsync<TState, TResult>(
        this DbContext dbContext,
        TState state,
        Func<TState, CancellationToken, Task<TResult>> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(operation);

        options ??= EfTransactionOptions.Default;

        // Check if we are already inside a transaction on THIS context instance.
        // If so, we must NOT use a new ExecutionStrategy, because nested retry logic 
        // inside an existing transaction is dangerous and can lead to data duplication.
        if (dbContext.Database.CurrentTransaction != null)
        {
            return ExecuteInTransactionInternalAsync(dbContext, state, operation, options, isNested: true, cancellationToken);
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return executionStrategy.ExecuteAsync(
            new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
            (context, st, ct) => ExecuteInTransactionInternalAsync(context, st.State, st.Operation, options, isNested: false, ct),
            async (_, st, ct) => new ExecutionResult<TResult>(await st.VerifySucceeded(st.State, ct).ConfigureAwait(false), st.Result),
            cancellationToken
        );
    }

    private static async Task<TResult> ExecuteInTransactionInternalAsync<TState, TResult>(
        DbContext context,
        TState state,
        Func<TState, CancellationToken, Task<TResult>> operation,
        IEfTransactionOptions options,
        bool isNested,
        CancellationToken ct)
    {
        if (context.ChangeTracker.HasChanges())
            throw new UnsavedChangesDetectedException(context, "Can't execute action, unsaved changes detected");

        // Set command timeout if specified
        var oldTimeout = context.Database.GetCommandTimeout();
        if (options.TimeoutInSeconds > 0)
            context.Database.SetCommandTimeout((int)options.TimeoutInSeconds);

        // Check for multi-context atomicity loss
        if (!isNested && CurrentLogicalTransaction.Value != null)
        {
            throw new InvalidOperationException(
                "Detected a nested call to ExecuteInTransactionAsync using a different DbContext instance. " +
                "Atomicity across multiple DbContext instances is not supported with IDbContextTransaction. " +
                "Use the same DbContext instance for the nested call. " +
                "If you only need retry logic for the second context without a transaction, use 'context.Database.CreateExecutionStrategy().ExecuteAsync()' directly.");
        }

        try
        {
            if (isNested)
            {
                var savepointName = $"sp_{Guid.NewGuid():N}";
                var currentTransaction = context.Database.CurrentTransaction!;
                var currentLevel = currentTransaction.GetDbTransaction().IsolationLevel;
                var requestedLevel = MapIsolationLevel(options.IsolationLevel);

                if (requestedLevel != IsolationLevel.Unspecified && currentLevel != IsolationLevel.Unspecified &&
                    GetIsolationLevelPriority(currentLevel) < GetIsolationLevelPriority(requestedLevel))
                {
                    throw new InvalidOperationException(
                        $"Can't participate in existing transaction with isolation level '{currentLevel}'. Requested level '{requestedLevel}' is stricter.");
                }

                // Create savepoint for nested atomicity
                await currentTransaction.CreateSavepointAsync(savepointName, ct).ConfigureAwait(false);

                try
                {
                    // Participate in existing transaction
                    var result = await operation(state, ct).ConfigureAwait(false);

                    if (context.ChangeTracker.HasChanges())
                    {
                        // Ensure changes are flushed before returning from nested call
                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    // Rollback only this nested part
                    try
                    {
                        await currentTransaction.RollbackToSavepointAsync(savepointName, ct).ConfigureAwait(false);
                    }
                    catch (Exception rollbackEx)
                    {
                        throw new AggregateException(
                            "Nested operation failed and savepoint rollback also failed", ex, rollbackEx);
                    }

                    throw;
                }
            }

            var dataIsolationLevel = MapIsolationLevel(options.IsolationLevel);

            await using var transaction = await context.Database.BeginTransactionAsync(dataIsolationLevel, ct).ConfigureAwait(false);
            CurrentLogicalTransaction.Value = transaction;

            try
            {
                var result = await operation(state, ct).ConfigureAwait(false);

                if (context.ChangeTracker.HasChanges())
                {
                    // In most cases, SaveChangesAsync should be called inside the operation delegate.
                    // However, we perform a final check to ensure all changes are flushed before commit.
                    await context.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                await transaction.CommitAsync(ct).ConfigureAwait(false);

                return result;
            }
            finally
            {
                CurrentLogicalTransaction.Value = null;
            }
        }
        catch (Exception ex)
            when (!isNested && ExecutionStrategy.CallOnWrappedException(ex,
                      exHandle => (exHandle as NpgsqlException)?.IsTransient == true || exHandle is TimeoutException))
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
    }

    private static readonly AsyncLocal<IDbContextTransaction?> CurrentLogicalTransaction = new();

    private static int GetIsolationLevelPriority(IsolationLevel level)
    {
        return level switch
        {
            IsolationLevel.Unspecified => 0,
            IsolationLevel.Chaos => 0,
            IsolationLevel.ReadUncommitted => 1,
            IsolationLevel.ReadCommitted => 2,
            IsolationLevel.RepeatableRead => 3,
            IsolationLevel.Snapshot => 4,
            IsolationLevel.Serializable => 5,
            _ => 2 // Default to ReadCommitted priority
        };
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