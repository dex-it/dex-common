using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef.Helpers;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore
{
    public static class ExecutionStrategyExtensions
    {
        [SuppressMessage("Design", "CA1062:Проверить аргументы или открытые методы")]
        public static async Task<TResult> ExecuteInTransactionScopeAsync<TState, TResult>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task<TResult>> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteAsync(
                new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
                async (_, st, ct) =>
                {
                    using var transactionScope = TransactionScopeHelper.CreateTransactionScope(transactionScopeOption, isolationLevel);
                    st.Result = await st.Operation(st.State, ct).ConfigureAwait(false);
                    transactionScope.Complete();

                    return st.Result;
                },
                async (_, st, ct) => new ExecutionResult<TResult>(await st.VerifySucceeded(st.State, ct).ConfigureAwait(false), st.Result),
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task<TResult> ExecuteInTransactionScopeAsync<TState, TResult>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task<TResult>> operation,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync<TState, TResult>(
                state,
                async (st, token) => await operation(st, token).ConfigureAwait(false),
                (_, _) => Task.FromResult(true),
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task<TResult> ExecuteInTransactionScopeAsync<TResult>(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task<TResult>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync<object, TResult>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task<TResult> ExecuteInTransactionScopeAsync<TResult>(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task<TResult>> operation,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync<object, TResult>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                (_, _) => Task.FromResult(true),
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        // without result
        public static async Task ExecuteInTransactionScopeAsync<TState>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync(
                state,
                async (st, ct) =>
                {
                    await operation(st, ct).ConfigureAwait(false);
                    return true;
                },
                verifySucceeded,
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task ExecuteInTransactionScopeAsync<TState>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync(
                state,
                async (st, ct) =>
                {
                    await operation(st, ct).ConfigureAwait(false);
                    return true;
                },
                (_, _) => Task.FromResult(true),
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task ExecuteInTransactionScopeAsync(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync<object>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
                transactionScopeOption,
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);

        public static async Task ExecuteInTransactionScopeAsync(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task> operation,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => await strategy.ExecuteInTransactionScopeAsync<object>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                (_, _) => Task.FromResult(true),
                transactionScopeOption,
                isolationLevel,
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