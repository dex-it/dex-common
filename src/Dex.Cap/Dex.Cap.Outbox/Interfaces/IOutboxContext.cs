using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxContext<out TState>
    {
        /// <summary>
        /// Outbox state context
        /// </summary>
        TState State { get; }

        /// <summary>
        /// Publish outbox message to queue. This method don't check Transaction, only append outbox message to change context.
        /// </summary>
        Task AddCommandAsync(IOutboxMessage outboxMessage, CancellationToken cancellationToken);
    }
}