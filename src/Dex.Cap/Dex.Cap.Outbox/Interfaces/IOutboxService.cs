using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxService
    {
        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        Task<Guid> ExecuteOperationAsync<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage;

        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        Task<Guid> ExecuteOperationAsync<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<TContext, TOutboxMessage> createOutboxData,
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