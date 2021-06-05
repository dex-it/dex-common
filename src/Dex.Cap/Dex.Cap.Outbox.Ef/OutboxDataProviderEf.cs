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

        public override async Task<Models.Outbox> Save(Models.Outbox outbox)
        {
            if (outbox == null) throw new ArgumentNullException(nameof(outbox));

            var entityEntry = _dbContext.Set<Models.Outbox>().Add(outbox);
            await _dbContext.SaveChangesAsync();

            return entityEntry.Entity;
        }

        public override async Task<Models.Outbox[]> GetWaitingMessages()
        {
            var outboxes = await _dbContext.Set<Models.Outbox>()
                .Where(o => o.Retries < _outboxOptions.Retries && (o.Status == OutboxMessageStatus.Failed || o.Status == OutboxMessageStatus.New))
                .OrderBy(o => o.Created)
                .Take(_outboxOptions.MessagesToProcess)
                .ToArrayAsync();

            return outboxes;
        }

        protected override async Task UpdateOutbox(Models.Outbox outbox)
        {
            _dbContext.Set<Models.Outbox>().Update(outbox);
            await _dbContext.SaveChangesAsync();
        }
    }
}