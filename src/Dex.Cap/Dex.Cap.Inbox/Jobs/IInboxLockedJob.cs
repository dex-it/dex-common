using System;
using System.Threading;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Jobs;

public interface IInboxLockedJob : IDisposable
{
    /// <summary>
    /// Ключ идемпотентности захваченной аренды.
    /// </summary>
    Guid LockId { get; }

    InboxEnvelope Envelope { get; }

    /// <summary>
    /// Отражает время жизни захваченной аренды.
    /// </summary>
    CancellationToken LockToken { get; }

    /// <summary>
    /// Таймаут операции.
    /// </summary>
    TimeSpan Timeout { get; }
}
