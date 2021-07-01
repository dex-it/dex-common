﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public abstract class BaseOutboxDataProvider : IOutboxDataProvider
    {
        public abstract Task ExecuteInTransaction(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
        public abstract Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);

        public abstract Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken);
        public abstract Task<OutboxEnvelope[]> GetWaitingMessages(CancellationToken cancellationToken);

        public virtual async Task Fail(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));

            outboxEnvelope.Status = OutboxMessageStatus.Failed;
            outboxEnvelope.Updated = DateTime.UtcNow;
            outboxEnvelope.Retries++;
            outboxEnvelope.ErrorMessage = errorMessage;
            outboxEnvelope.Error = exception?.ToString();

            await SaveChanges(outboxEnvelope, cancellationToken);
        }

        public virtual async Task Succeed(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));

            outboxEnvelope.Status = OutboxMessageStatus.Succeeded;
            outboxEnvelope.Updated = DateTime.UtcNow;
            outboxEnvelope.Retries++;

            await SaveChanges(outboxEnvelope, cancellationToken);
        }

        protected abstract Task SaveChanges(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
    }
}