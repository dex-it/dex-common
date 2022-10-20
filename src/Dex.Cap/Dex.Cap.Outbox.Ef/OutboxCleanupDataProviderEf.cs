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
            const int limit = 1000;

            int removedCount = 0;
            Guid[] finishedMessages;
            do
            {
                finishedMessages = await GetFinishedMessages(limit, olderThan, cancellationToken).ConfigureAwait(false);

                if (finishedMessages.Length > 0)
                {
                    _logger.LogInformation("Found {Count} messages older than {OlderThan}", finishedMessages.Length, olderThan);

                    foreach (var messageId in finishedMessages)
                    {
                        await DeleteMessage(messageId, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Deleted old message {MessageId}", messageId);
                    }

                    removedCount += finishedMessages.Length;
                }
                else
                {
                    _logger.LogTrace("No messages older than {OlderThan}", olderThan);
                }
            } while (finishedMessages.Length == limit);

            return removedCount;
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

            await strategy.ExecuteInTransactionAsync((_dbContext, _logger, messageId), static async (state, ct) =>
                    {
                        var (dbContext, logger, messageId) = state;

                        var lockedJob = await dbContext.Set<OutboxEnvelope>()
                            .Where(x => x.Id == messageId)
                            .FirstOrDefaultAsync(ct)
                            .ConfigureAwait(false);

                        if (lockedJob != null)
                        {
                            dbContext.Set<OutboxEnvelope>().Remove(lockedJob);

                            try
                            {
                                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                            }
                            catch (DbUpdateException e)
                            {
                                logger.LogInformation(e, "Job {JobId} has already been deleted", lockedJob.Id);
                            }
                        }
                    },
                    static async (state, ct) =>
                    {
                        var (dbContext, _, messageId) = state;
                        bool exists = await dbContext.Set<OutboxEnvelope>().AnyAsync(x => x.Id == messageId, ct).ConfigureAwait(false);
                        return !exists;
                    },
                    IsolationLevel.RepeatableRead,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}