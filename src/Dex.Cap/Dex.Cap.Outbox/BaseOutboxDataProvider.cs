using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public abstract class BaseOutboxDataProvider : IOutboxDataProvider
    {
        public abstract Task ExecuteInTransactionAsync(Guid correlationId, Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
        public abstract Task<OutboxEnvelope> AddAsync(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken);
        public abstract Task<bool> IsExistsAsync(Guid correlationId, CancellationToken cancellationToken);
        public abstract IAsyncEnumerable<IOutboxLockedJob> GetWaitingJobs(CancellationToken cancellationToken);

        public virtual async Task FailAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken, string? errorMessage = null, Exception? exception = null)
        {
            if (outboxJob == null)
            {
                throw new ArgumentNullException(nameof(outboxJob));
            }

            outboxJob.Envelope.Status = OutboxMessageStatus.Failed;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = errorMessage;
            outboxJob.Envelope.Error = exception?.ToString();

            await CompleteJobAsync(outboxJob, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task SucceedAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken)
        {
            if (outboxJob == null)
            {
                throw new ArgumentNullException(nameof(outboxJob));
            }

            outboxJob.Envelope.Status = OutboxMessageStatus.Succeeded;
            outboxJob.Envelope.Updated = DateTime.UtcNow;
            outboxJob.Envelope.Retries++;
            outboxJob.Envelope.ErrorMessage = null;
            outboxJob.Envelope.Error = null;

            await CompleteJobAsync(outboxJob, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task CompleteJobAsync(IOutboxLockedJob outboxJob, CancellationToken cancellationToken);

        public virtual bool IsDbTransientError(Exception ex)
        {
            return false;
        }

        public virtual Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}