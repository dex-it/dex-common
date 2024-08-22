using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxContext<out TDbContext, out TState>
    {
        /// <summary>
        /// DbContext
        /// </summary>
        TDbContext DbContext { get; }

        /// <summary>
        /// Outbox state context
        /// </summary>
        TState State { get; }

        /// <summary>
        /// Publish outbox message to queue.
        /// This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task EnqueueAsync(IOutboxMessage outboxMessage, DateTime? startAtUtc = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets outbox type discriminator
        /// </summary>
        IOutboxTypeDiscriminator GetDiscriminator();
    }
}