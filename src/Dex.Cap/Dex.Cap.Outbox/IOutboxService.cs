using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxService
    {
        /// <summary>
        /// Execute operation and publish message to outbox queue into transaction
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="operation">database change async action</param>
        /// <param name="message">outbox message</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<Guid> ExecuteOperation<T>(Guid correlationId, Func<CancellationToken, Task> operation, T message, CancellationToken cancellationToken)
            where T : IOutboxMessage;

        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context 
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<Guid> Enqueue<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage;
        
        /// <summary>
        /// Perform only publish outbox message to queue. This method don't check Transaction, only append outbox message to change context 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<Guid> Enqueue<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage;

        /// <summary>
        /// Check if operation with correlationId already exists
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsOperationExists(Guid correlationId, CancellationToken cancellationToken);
    }
}