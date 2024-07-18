using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Ef
{
    internal sealed class OutboxCleanupDataProviderEf<TDbContext> : BaseCleanupProvider where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ILogger _logger;

        public OutboxCleanupDataProviderEf(TDbContext dbContext,
            ILogger<OutboxCleanupDataProviderEf<TDbContext>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            const OutboxMessageStatus status = OutboxMessageStatus.Succeeded;
            const int limit = 1000;
            var total = 0;

            var stamp = DateTime.UtcNow.Subtract(olderThan);

            _logger.LogDebug("Cleaning up outbox messages older than {Timestamp} with status {Status}", stamp, status);

            var cont = true;
            while (cont)
            {
                var affected = await _dbContext.Set<OutboxEnvelope>()
                    .Where(om => om.CreatedUtc < stamp && om.Status == status)
                    .OrderBy(om => om.CreatedUtc)
                    .Take(limit)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                cont = affected > 0;
                total += affected;
            }

            return total;
        }
    }
}