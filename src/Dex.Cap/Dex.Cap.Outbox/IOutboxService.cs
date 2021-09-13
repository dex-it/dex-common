using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService
    {
        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction.
        /// </summary>
        Task<Guid> ExecuteOperationAsync<T>(Guid correlationId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            where T : IOutboxMessage;

        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage;
        
        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage;

        /// <summary>
        /// Check if operation with correlationId already exists.
        /// </summary>
        Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken);
    }
}