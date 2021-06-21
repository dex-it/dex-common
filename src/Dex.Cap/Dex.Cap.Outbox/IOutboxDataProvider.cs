using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public interface IOutboxDataProvider
    {
        Task ExecuteInTransaction(Guid correlationId, Action<CancellationToken> operation, CancellationToken cancellationToken);

        Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        Task Fail(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null);
        Task Succeed(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        Task<OutboxEnvelope[]> GetWaitingMessages(CancellationToken cancellationToken);
        Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);
    }
}