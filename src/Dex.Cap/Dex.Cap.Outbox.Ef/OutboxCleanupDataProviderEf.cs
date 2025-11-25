using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Ef;

internal sealed class OutboxCleanupDataProviderEf<TDbContext>(
    TDbContext dbContext,
    ILogger<OutboxCleanupDataProviderEf<TDbContext>> logger,
    IOutboxTypeDiscriminatorProvider discriminatorProvider) : BaseCleanupProvider where TDbContext : DbContext
{
    public override async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        const OutboxMessageStatus statusSucceeded = OutboxMessageStatus.Succeeded;
        const int limit = 1000;
        var total = 0;

        var stamp = DateTime.UtcNow.Subtract(olderThan);

        logger.LogDebug("Cleaning up outbox messages older than {Timestamp} with status {Status}", stamp, statusSucceeded);

        var immediatelyDeletableMessages = discriminatorProvider.ImmediatelyDeletableMessages;

        var cont = true;
        while (cont)
        {
            var affected = await dbContext.Set<OutboxEnvelope>()
                .Where(om => om.Status == statusSucceeded)
                .Where(om => om.CreatedUtc < stamp || immediatelyDeletableMessages.Contains(om.MessageType))
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