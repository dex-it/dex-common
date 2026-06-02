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
    /// Recommended for CQRS (Read/Write) and multi-database scenarios to avoid "Ambient transaction detected" errors and DTC escalation.
    /// Supports nested calls (reentrancy) by participating in the existing transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For nested calls within the same <see cref="DbContext"/> instance, the operation participates in the existing transaction.
    /// This implementation follows the "All-or-Nothing" principle: any exception in a nested call will likely 
    /// poison the transaction (especially in PostgreSQL) and require a full rollback and retry of the root operation.
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
    /// If a failure occurs, the strategy will retry the ENTIRE root block from the beginning. 
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await context.ExecuteInTransactionAsync(async ct => {
    ///     await context.Users.AddAsync(new User { Name = "User1" }, ct);
    ///     
    ///     // Nested call joins the same transaction
    ///     await context.ExecuteInTransactionAsync(async ctInner => {
    ///         await context.Orders.AddAsync(new Order { Total = 100 }, ctInner);
    ///     }, ct);
    ///     
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

    // ReSharper disable once CognitiveComplexity
    private static async Task<TResult> ExecuteInTransactionInternalAsync<TState, TResult>(
        DbContext context,
        TState state,
        Func<TState, CancellationToken, Task<TResult>> operation,
        IEfTransactionOptions options,
        bool isNested,
        CancellationToken ct)
    {
        // Check for unsaved changes ONLY at the root level before starting a new transaction.
        // Nested calls are expected to have changes from the outer scope.
        if (!isNested && context.ChangeTracker.HasChanges())
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
                var currentTransaction = context.Database.CurrentTransaction!;
                var currentLevel = currentTransaction.GetDbTransaction().IsolationLevel;
                var requestedLevel = MapIsolationLevel(options.IsolationLevel);

                if (requestedLevel != IsolationLevel.Unspecified && currentLevel != IsolationLevel.Unspecified &&
                    GetIsolationLevelPriority(currentLevel) < GetIsolationLevelPriority(requestedLevel))
                {
                    throw new InvalidOperationException(
                        $"Can't participate in existing transaction with isolation level '{currentLevel}'. Requested level '{requestedLevel}' is stricter.");
                }

                // Participate in existing transaction
                var result = await operation(state, ct).ConfigureAwait(false);

                if (context.ChangeTracker.HasChanges())
                {
                    // Ensure changes are flushed before returning from nested call
                    await context.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                return result;
            }

            var dataIsolationLevel = MapIsolationLevel(options.IsolationLevel);

#pragma warning disable CA2007 // await using не поддерживает ConfigureAwait без потери типа; SynchronizationContext в ASP.NET Core отсутствует
            await using var transaction = await context.Database.BeginTransactionAsync(dataIsolationLevel, ct).ConfigureAwait(false);
#pragma warning restore CA2007

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
            // If any error occurs in the root block, we clear the tracker to ensure a clean state for the next retry attempt.
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