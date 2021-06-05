using System;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxDataProvider
    {
        Task<Models.OutboxEnvelope> Save(Models.OutboxEnvelope outboxEnvelope);
        Task<Models.OutboxEnvelope[]> GetWaitingMessages();
        Task Fail(Models.OutboxEnvelope outboxEnvelope, string? errorMessage = null, Exception? exception = null);
        Task Succeed(Models.OutboxEnvelope outboxEnvelope);
    }
}