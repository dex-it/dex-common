using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Dex.Cap.Common.Ef.Extensions;

public static class DbContextExecuteInTransactionExtensions
{
    // ReSharper disable once CognitiveComplexity
    public static Task<TResult> ExecuteInTransactionScopeAsync<TState, TResult>(
        this DbContext dbContext,
        TState state,
        Func<TState, CancellationToken, Task<TResult>> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        options ??= new EfTransactionOptions();

        return dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
            async (context, st, ct) =>
            {
                if (dbContext.ChangeTracker.HasChanges())
                    throw new UnsavedChangesDetectedException(context,
                        "Can't execute action, unsaved changes detected");

                try
                {
                    var timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);

                    using var transactionScope = TransactionScopeHelper
                        .CreateTransactionScope(options.TransactionScopeOption, options.IsolationLevel, timeout);

                    st.Result = await st.Operation(st.State, ct).ConfigureAwait(false);

                    if (context.ChangeTracker.HasChanges())
                        throw new UnsavedChangesDetectedException(context,
                            "Can't complete action, unsaved changes detected");

                    transactionScope.Complete();

                    return st.Result;
                }
                catch (Exception ex)
                    when (ExecutionStrategy.CallOnWrappedException(ex,
                              exHandle => (exHandle as NpgsqlException)?.IsTransient == true ||
                                          exHandle is TimeoutException))
                {
                    // Важно: очищать ChangeTracker только при явном указании
                    // т.к. в случае ретрая стратегии можем потерять информацию о прочитанных данных, вызывающего кода
                    if (options.ClearChangeTrackerOnRetry)
                        context.ChangeTracker.Clear();

                    throw;
                }
            },
            async (_, st, ct) =>
                new ExecutionResult<TResult>(await st.VerifySucceeded(st.State, ct).ConfigureAwait(false),
                    st.Result),
            cancellationToken
        );
    }

    public static Task<TResult> ExecuteInTransactionScopeAsync<TResult>(
        this DbContext dbContext,
        Func<CancellationToken, Task<TResult>> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = default,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionScopeAsync<object, TResult>(
            default!,
            async (_, token) => await operation(token).ConfigureAwait(false),
            async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
            options,
            cancellationToken
        );

    // without result
    public static Task ExecuteInTransactionScopeAsync<TState>(
        this DbContext dbContext,
        TState state,
        Func<TState, CancellationToken, Task> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = default,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionScopeAsync(
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

    public static Task ExecuteInTransactionScopeAsync(
        this DbContext dbContext,
        Func<CancellationToken, Task> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        IEfTransactionOptions? options = default,
        CancellationToken cancellationToken = default)
        => dbContext.ExecuteInTransactionScopeAsync<object>(
            default!,
            async (_, token) => await operation(token).ConfigureAwait(false),
            async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
            options,
            cancellationToken
        );
}