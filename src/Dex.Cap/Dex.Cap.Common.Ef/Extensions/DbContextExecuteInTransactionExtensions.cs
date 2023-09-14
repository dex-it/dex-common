using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dex.Cap.Common.Ef.Extensions
{
    public static class DbContextExecuteInTransactionExtensions
    {
        [SuppressMessage("Design", "CA1062:Проверить аргументы или открытые методы")]
        public static async Task<TResult> ExecuteInTransactionScopeAsync<TState, TResult>(
            this DbContext dbContext,
            TState state,
            Func<TState, CancellationToken, Task<TResult>> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
                async (context, st, ct) =>
                {
                    if (dbContext.ChangeTracker.HasChanges())
                        throw new UnsavedChangesDetectedException(context,
                            "Can't execute action, unsaved changes detected");

                    var timeout = TimeSpan.FromSeconds(timeoutInSeconds);
                    using var transactionScope =
                        TransactionScopeHelper.CreateTransactionScope(transactionScopeOption, isolationLevel, timeout);
                    st.Result = await st.Operation(st.State, ct).ConfigureAwait(false);

                    if (context.ChangeTracker.HasChanges())
                        throw new UnsavedChangesDetectedException(context,
                            "Can't complete action, unsaved changes detected");

                    transactionScope.Complete();

                    return st.Result;
                },
                async (_, st, ct) => new ExecutionResult<TResult>(await st.VerifySucceeded(st.State, ct).ConfigureAwait(false), st.Result),
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task<TResult> ExecuteInTransactionScopeAsync<TResult>(
            this DbContext dbContext,
            Func<CancellationToken, Task<TResult>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteInTransactionScopeAsync<object, TResult>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
                transactionScopeOption,
                isolationLevel,
                timeoutInSeconds,
                cancellationToken
            ).ConfigureAwait(false);

        // without result
        public static async Task ExecuteInTransactionScopeAsync<TState>(
            this DbContext dbContext,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteInTransactionScopeAsync(
                state,
                async (st, ct) =>
                {
                    await operation(st, ct).ConfigureAwait(false);
                    return true;
                },
                verifySucceeded,
                transactionScopeOption,
                isolationLevel,
                timeoutInSeconds,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task ExecuteInTransactionScopeAsync(
            this DbContext dbContext,
            Func<CancellationToken, Task> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteInTransactionScopeAsync<object>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
                transactionScopeOption,
                isolationLevel,
                timeoutInSeconds,
                cancellationToken
            ).ConfigureAwait(false);

        private sealed class ExecutionStateAsync<TState, TResult>
        {
            public ExecutionStateAsync(
                Func<TState, CancellationToken, Task<TResult>> operation,
                Func<TState, CancellationToken, Task<bool>> verifySucceeded,
                TState state)
            {
                Operation = operation;
                VerifySucceeded = verifySucceeded;
                State = state;
            }

            public Func<TState, CancellationToken, Task<TResult>> Operation { get; }
            public Func<TState, CancellationToken, Task<bool>> VerifySucceeded { get; }
            public TState State { get; }
            public TResult Result { get; set; } = default!;
        }
    }
}