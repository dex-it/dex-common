using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Ef
{
    public class OutboxDataProviderEf<TDbContext> : BaseOutboxDataProvider where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly OutboxOptions _outboxOptions;

        public OutboxDataProviderEf(TDbContext dbContext, IOptions<OutboxOptions> outboxOptions)
        {
            _dbContext = dbContext;
            _outboxOptions = outboxOptions.Value;
        }

        public override async Task<OutboxEnvelope> Save(OutboxEnvelope outboxEnvelope)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));

            var entityEntry = _dbContext.Set<OutboxEnvelope>().Add(outboxEnvelope);
            await _dbContext.SaveChangesAsync();

            return entityEntry.Entity;
        }

        public override async Task<OutboxEnvelope[]> GetWaitingMessages()
        {
            var outboxes = await _dbContext.Set<OutboxEnvelope>()
                .Where(o => o.Retries < _outboxOptions.Retries && (o.Status == OutboxMessageStatus.Failed || o.Status == OutboxMessageStatus.New))
                .OrderBy(o => o.Created)
                .Take(_outboxOptions.MessagesToProcess)
                .ToArrayAsync();

            return outboxes;
        }

        protected override async Task UpdateOutbox(OutboxEnvelope outboxEnvelope)
        {
            _dbContext.Set<OutboxEnvelope>().Update(outboxEnvelope);
            await _dbContext.SaveChangesAsync();
        }
    }
}