using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Ef
{
    internal sealed class OutboxCleanupDataProviderEf<TDbContext> : BaseCleanupProvider where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ILogger _logger;

        public OutboxCleanupDataProviderEf(TDbContext dbContext, ILogger<OutboxCleanupDataProviderEf<TDbContext>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            int removedCount = 0;

        Repeat:
            var finishedMessages = await GetFinishedMessages(limit: 1000, olderThan, cancellationToken).ConfigureAwait(false);

            if (finishedMessages.Length == 0)
            {
                _logger.LogTrace("No messages older than {Span}", olderThan);
                return removedCount;
            }

            _logger.LogInformation("Found {Count} messages older than {Span}", olderThan);

            foreach (var messageId in finishedMessages)
            {
                await DeleteMessage(messageId, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Deleted old message {MessageId}", messageId);
            }

            removedCount += finishedMessages.Length;
            goto Repeat;
        }

        private async Task<Guid[]> GetFinishedMessages(int limit, TimeSpan olderThan, CancellationToken cancellationToken)
        {
            var finishedMessages = await _dbContext.Set<OutboxEnvelope>()
                .Where(o => o.Status == OutboxMessageStatus.Succeeded && o.CreatedUtc + olderThan < DateTime.UtcNow)
                .OrderBy(o => o.CreatedUtc)
                .Take(limit)
                .AsNoTracking()
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            return finishedMessages;
        }

        private async Task DeleteMessage(Guid messageId, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransactionAsync((_dbContext, messageId), static async (state, ct) =>
            {
                var (dbContext, messageId) = state;

                var lockedJob = await dbContext.Set<OutboxEnvelope>()
                    .Where(x => x.Id == messageId)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (lockedJob != null)
                {
                    dbContext.Set<OutboxEnvelope>().Remove(lockedJob);
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                }
            },
            static async (state, ct) =>
            {
                var (dbContext, messageId) = state;
                bool exists = await dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == messageId, ct).ConfigureAwait(false);
                return !exists;
            },
            IsolationLevel.RepeatableRead,
            cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
