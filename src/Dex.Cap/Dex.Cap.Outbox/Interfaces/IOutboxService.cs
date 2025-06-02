using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxService
    {
        /// <summary>
        /// Outbox Type Discriminator
        /// </summary>
        IOutboxTypeDiscriminator Discriminator { get; }

        /// <summary>
        /// Id корреляции
        /// </summary>
        Guid CorrelationId { get; }

        /// <summary>
        /// Perform only publish outbox message to queue.
        /// This method don't check Transaction, only append outbox message to change context.
        /// NOTE. LockTimeout must be greater time of process message, against it lead to cycle process.
        /// Default value is 30sec. Minimum value 10 sec.
        /// </summary>
        Task<Guid> EnqueueAsync<T>(
            T message,
            Guid? correlationId = null,
            DateTime? startAtUtc = null,
            TimeSpan? lockTimeout = null,
            CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Check if operation with correlationId already exists.
        /// </summary>
        Task<bool> IsOperationExistsAsync(
            Guid? correlationId = null,
            CancellationToken cancellationToken = default);
    }
}