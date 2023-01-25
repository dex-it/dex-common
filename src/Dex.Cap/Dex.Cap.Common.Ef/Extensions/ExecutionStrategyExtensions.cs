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
        public static Task<TResult> ExecuteInTransactionScopeAsync<TState, TResult>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task<TResult>> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => strategy.ExecuteAsync(
                new ExecutionStateAsync<TState, TResult>(operation, verifySucceeded, state),
                async (_, s, ct) =>
                {
                    using var transactionScope = TransactionScopeHelper.CreateTransactionScope(TransactionScopeOption.Required, isolationLevel);
                    s.Result = await s.Operation(s.State, ct).ConfigureAwait(false);
                    transactionScope.Complete();

                    return s.Result;
                },
                async (_, s, ct) => new ExecutionResult<TResult>(await s.VerifySucceeded(s.State, ct).ConfigureAwait(false), s.Result),
                cancellationToken);

        public static Task<TResult> ExecuteInTransactionScopeAsync<TResult>(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task<TResult>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => strategy.ExecuteInTransactionScopeAsync<object, TResult>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                (_, _) => Task.FromResult(true),
                isolationLevel,
                cancellationToken);

        public static Task ExecuteInTransactionScopeAsync(
            this IExecutionStrategy strategy,
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => strategy.ExecuteInTransactionScopeAsync<object>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                (_, _) => Task.FromResult(true),
                isolationLevel,
                cancellationToken);

        public static Task ExecuteInTransactionScopeAsync<TState>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => strategy.ExecuteInTransactionScopeAsync(
                state,
                async (s, ct) =>
                {
                    await operation(s, ct).ConfigureAwait(false);
                    return true;
                },
                verifySucceeded,
                isolationLevel,
                cancellationToken);

        public static Task ExecuteInTransactionScopeAsync<TState>(
            this IExecutionStrategy strategy,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
            => strategy.ExecuteInTransactionScopeAsync(
                state,
                async (s, ct) =>
                {
                    await operation(s, ct).ConfigureAwait(false);
                    return true;
                },
                (_, _) => Task.FromResult(true),
                isolationLevel,
                cancellationToken);

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