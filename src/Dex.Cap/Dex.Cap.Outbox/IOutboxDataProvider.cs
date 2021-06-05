using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public interface IOutboxDataProvider
    {
        Task<OutboxEnvelope> Save(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        Task<Models.OutboxEnvelope[]> GetWaitingMessages();
        Task Fail(Models.OutboxEnvelope outboxEnvelope, string? errorMessage = null, Exception? exception = null);
        Task Succeed(Models.OutboxEnvelope outboxEnvelope);
    }
}