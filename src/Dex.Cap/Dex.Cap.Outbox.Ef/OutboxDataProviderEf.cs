using System;
using System.Linq;
using System.Threading;
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

        public override async Task ExecuteInTransaction(Guid correlationId, Action<CancellationToken> operation, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteInTransactionAsync(() =>
            {
                operation(cancellationToken);
            }, () => IsExists(correlationId, cancellationToken));
        }

        public override async Task<OutboxEnvelope> Add(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            if (outboxEnvelope == null) throw new ArgumentNullException(nameof(outboxEnvelope));

            var entityEntry = await _dbContext.Set<OutboxEnvelope>().AddAsync(outboxEnvelope, cancellationToken);
            return entityEntry.Entity;
        }

        public override async Task<OutboxEnvelope[]> GetWaitingMessages(CancellationToken cancellationToken)
        {
            var outboxes = await _dbContext.Set<OutboxEnvelope>()
                .Where(o => o.Retries < _outboxOptions.Retries && (o.Status == OutboxMessageStatus.Failed || o.Status == OutboxMessageStatus.New))
                .OrderBy(o => o.Created)
                .Take(_outboxOptions.MessagesToProcess)
                .ToArrayAsync(cancellationToken: cancellationToken);

            return outboxes;
        }

        public override Task<bool> IsExists(Guid correlationId, CancellationToken cancellationToken)
        {
            return _dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == correlationId, cancellationToken: cancellationToken);
        }

        protected override async Task SaveChanges(OutboxEnvelope outboxEnvelope, CancellationToken cancellationToken)
        {
            _dbContext.Set<OutboxEnvelope>().Update(outboxEnvelope);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}