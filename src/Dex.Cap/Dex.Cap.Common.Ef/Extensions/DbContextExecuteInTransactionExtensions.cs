using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef.Exceptions;
using Dex.Cap.Common.Ef.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Dex.Cap.Common.Ef.Extensions
{
    public static class DbContextExecuteInTransactionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="state"></param>
        /// <param name="operation">Запрещено вызывать SaveChanges</param>
        /// <param name="verifySucceeded"></param>
        /// <param name="transactionScopeOption"></param>
        /// <param name="isolationLevel"></param>
        /// <param name="timeoutInSeconds"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TState"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        /// <exception cref="UnsavedChangesDetectedException"></exception>
        [SuppressMessage("Design", "CA1062:Проверить аргументы или открытые методы")]
        public static async Task<TResult> ExecuteAndSaveInTransactionAsync<TState, TResult>(
            this DbContext dbContext,
            TState state,
            Func<TState, CancellationToken, Task<TResult>> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
        {
            if (dbContext.ChangeTracker.HasChanges())
                throw new UnsavedChangesDetectedException(dbContext, "Can't execute action, unsaved changes detected");

            var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

            // чтобы все чтения были в одном соединении
            // if (dbContext.Database.GetDbConnection()?.State != ConnectionState.Open)
            // {
            //     await dbContext.Database.GetDbConnection().OpenAsync(cancellationToken);
            //     //await dbContext.Database.OpenConnectionAsync(cancellationToken);
            // }

            var result = await operation(state, cancellationToken).ConfigureAwait(false);

            var strategy = dbContext.Database.CreateExecutionStrategy();
            result = await strategy.ExecuteAsync(
                (verifySucceeded, state, result, transactionScopeOption, isolationLevel, timeout),
                async static (context, st, ct) =>
                {
                    // открываем скоуп транзакции или привязываемся к существующему
                    using var transactionScope = TransactionScopeHelper.CreateTransactionScope(st.transactionScopeOption, st.isolationLevel, st.timeout);
                    await context.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct).ConfigureAwait(false);
                    //throw new TimeoutException("test");
                    transactionScope.Complete();
                    return st.result;
                },
                async static (_, st, ct) => new ExecutionResult<TResult>(await st.verifySucceeded(st.state, ct).ConfigureAwait(false), st.result),
                cancellationToken
            ).ConfigureAwait(false);

            // await strategy.ExecuteInTransactionAsync( //todo не получится привязаться к внешнему TransactionScope
            //     (dbContext, verifySucceeded, state),
            //     operation: async static (st, ct) => await st.dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct).ConfigureAwait(false),
            //     verifySucceeded: async static (st, ct) => await st.verifySucceeded(st.state, ct).ConfigureAwait(false),
            //     cancellationToken).ConfigureAwait(false);

            dbContext.ChangeTracker.AcceptAllChanges();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="operation">Запрещено вызывать SaveChanges</param>
        /// <param name="verifySucceeded"></param>
        /// <param name="transactionScopeOption"></param>
        /// <param name="isolationLevel"></param>
        /// <param name="timeoutInSeconds"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static async Task<TResult> ExecuteAndSaveInTransactionAsync<TResult>(
            this DbContext dbContext,
            Func<CancellationToken, Task<TResult>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteAndSaveInTransactionAsync<object, TResult>(
                default!,
                async (_, token) => await operation(token).ConfigureAwait(false),
                async (_, token) => await verifySucceeded(token).ConfigureAwait(false),
                transactionScopeOption,
                isolationLevel,
                timeoutInSeconds,
                cancellationToken
            ).ConfigureAwait(false);

        /// <summary>
        /// without result
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="state"></param>
        /// <param name="operation">Запрещено вызывать SaveChanges</param>
        /// <param name="verifySucceeded"></param>
        /// <param name="transactionScopeOption"></param>
        /// <param name="isolationLevel"></param>
        /// <param name="timeoutInSeconds"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TState"></typeparam>
        public static async Task ExecuteAndSaveInTransactionAsync<TState>(
            this DbContext dbContext,
            TState state,
            Func<TState, CancellationToken, Task> operation,
            Func<TState, CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteAndSaveInTransactionAsync(
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="operation">Запрещено вызывать SaveChanges</param>
        /// <param name="verifySucceeded"></param>
        /// <param name="transactionScopeOption"></param>
        /// <param name="isolationLevel"></param>
        /// <param name="timeoutInSeconds"></param>
        /// <param name="cancellationToken"></param>
        public static async Task ExecuteAndSaveInTransactionAsync(
            this DbContext dbContext,
            Func<CancellationToken, Task> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            uint timeoutInSeconds = 60,
            CancellationToken cancellationToken = default)
            => await dbContext.ExecuteAndSaveInTransactionAsync<object>(
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