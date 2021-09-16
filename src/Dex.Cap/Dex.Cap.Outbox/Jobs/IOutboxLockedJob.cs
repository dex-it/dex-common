using System;
using System.Threading;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Jobs
{
    public interface IOutboxLockedJob : IDisposable
    {
        /// <summary>
        /// Ключ идемпотентности.
        /// </summary>
        Guid LockId { get; }

        OutboxEnvelope Envelope { get; }

        /// <summary>
        /// Отражает время жизни захваченной блокировки.
        /// </summary>
        CancellationToken LockToken { get; }
    }
}
