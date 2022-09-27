using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxService<out TDbContext>
    {
        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="usefulAction">A delegate representing an executable operation that returns the result of type <typeparamref name="TDataContext" />.</param>
        /// <param name="createOutboxData">A delegate representing an executable operation that returns the result of type <typeparamref name="TOutboxMessage" />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="TDataContext">The return type of <paramref name="usefulAction" />.</typeparam>
        /// <typeparam name="TOutboxMessage">The return type of <paramref name="createOutboxData" />.</typeparam>
        /// <returns>CorrelationId</returns>
        Task<Guid> ExecuteOperationAsync<TDataContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, IOutboxContext<TDbContext, object?>, Task<TDataContext>> usefulAction,
            Func<CancellationToken, TDataContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage
        {
            return ExecuteOperationAsync(correlationId, default, usefulAction, createOutboxData, cancellationToken);
        }

        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="state">The state that will be passed to the usefulAction.</param>
        /// <param name="usefulAction">A delegate representing an executable operation that returns the result of type <typeparamref name="TDataContext" />.</param>
        /// <param name="createOutboxData">A delegate representing an executable operation that returns the result of type <typeparamref name="TOutboxMessage" />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <typeparam name="TDataContext">The return type of <paramref name="usefulAction" />.</typeparam>
        /// <typeparam name="TOutboxMessage">The return type of <paramref name="createOutboxData" />.</typeparam>
        /// <returns>CorrelationId</returns>
        Task<Guid> ExecuteOperationAsync<TState, TDataContext, TOutboxMessage>(Guid correlationId, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task<TDataContext>> usefulAction,
            Func<CancellationToken, TDataContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage;

        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="usefulAction">A delegate representing an executable operation that returns the result of type <typeparamref name="TDataContext" />.</param>
        /// <param name="createOutboxData">A delegate representing an executable operation that returns the result of type <typeparamref name="TOutboxMessage" />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="TDataContext">The return type of <paramref name="usefulAction" />.</typeparam>
        /// <typeparam name="TOutboxMessage">The return type of <paramref name="createOutboxData" />.</typeparam>
        /// <returns>CorrelationId</returns>
        Task<Guid> ExecuteOperationAsync<TDataContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, IOutboxContext<TDbContext, object?>, Task<TDataContext>> usefulAction,
            Func<TDataContext, TOutboxMessage> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage
        {
            return ExecuteOperationAsync(correlationId, default, usefulAction, createOutboxData, cancellationToken);
        }

        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="state">The state that will be passed to the usefulAction.</param>
        /// <param name="usefulAction">A delegate representing an executable operation that returns the result of type <typeparamref name="TDataContext" />.</param>
        /// <param name="createOutboxData">A delegate representing an executable operation that returns the result of type <typeparamref name="TOutboxMessage" />.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <typeparam name="TState">The type of the state.</typeparam>
        /// <typeparam name="TDataContext">The return type of <paramref name="usefulAction" />.</typeparam>
        /// <typeparam name="TOutboxMessage">The return type of <paramref name="createOutboxData" />.</typeparam>
        /// <returns>CorrelationId</returns>
        Task<Guid> ExecuteOperationAsync<TState, TDataContext, TOutboxMessage>(Guid correlationId, TState state,
            Func<CancellationToken, IOutboxContext<TDbContext, TState>, Task<TDataContext>> usefulAction,
            Func<TDataContext, TOutboxMessage> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage;

        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, CancellationToken cancellationToken = default) where T : IOutboxMessage;

        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(T message, CancellationToken cancellationToken = default) where T : IOutboxMessage;

        /// <summary>
        /// Check if operation with correlationId already exists.
        /// </summary>
        Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken = default);
    }
}