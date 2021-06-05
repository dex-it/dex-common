using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public abstract class BaseOutboxDataProvider : IOutboxDataProvider
    {
        public abstract Task<OutboxEnvelope> Save(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        public abstract Task<OutboxEnvelope[]> GetWaitingMessages();

        public virtual async Task Fail(OutboxEnvelope outboxEnvelope, string? errorMessage = null, Exception? exception = null)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));
            
            outboxEnvelope.Status = OutboxMessageStatus.Failed;
            outboxEnvelope.Updated = DateTime.UtcNow;
            outboxEnvelope.Retries++;
            outboxEnvelope.ErrorMessage = errorMessage;
            outboxEnvelope.Error = exception?.ToString();

            await UpdateOutbox(outboxEnvelope);
        }

        public virtual async Task Succeed(OutboxEnvelope outboxEnvelope)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));
            
            outboxEnvelope.Status = OutboxMessageStatus.Succeeded;
            outboxEnvelope.Updated = DateTime.UtcNow;
            outboxEnvelope.Retries++;

            await UpdateOutbox(outboxEnvelope);
        }

        protected abstract Task UpdateOutbox(OutboxEnvelope outboxEnvelope);
    }
}