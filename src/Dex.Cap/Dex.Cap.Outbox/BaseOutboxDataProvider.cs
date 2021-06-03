using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    public abstract class BaseOutboxDataProvider<TDbContext> : IOutboxDataProvider<TDbContext>
    {
        public abstract Task<Models.Outbox> Save(Models.Outbox outbox);

        public abstract Task<Models.Outbox[]> GetWaitingMessages();

        public virtual async Task Fail(Models.Outbox outbox, string errorMessage = null, Exception exception = null)
        {
            outbox.Status = OutboxMessageStatus.Failed;
            outbox.Updated = DateTime.UtcNow;
            outbox.Retries++;
            outbox.ErrorMessage = errorMessage;
            outbox.Error = exception?.ToString();

            await UpdateOutbox(outbox);
        }

        public virtual async Task Succeed(Models.Outbox outbox)
        {
            outbox.Status = OutboxMessageStatus.Succeeded;
            outbox.Updated = DateTime.UtcNow;
            outbox.Retries++;

            await UpdateOutbox(outbox);
        }

        protected abstract Task UpdateOutbox(Models.Outbox outbox);
    }
}