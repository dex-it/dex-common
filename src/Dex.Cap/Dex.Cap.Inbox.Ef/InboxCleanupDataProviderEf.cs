using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox.Ef;

internal sealed class InboxCleanupDataProviderEf<TDbContext>(TDbContext dbContext, ILogger<InboxCleanupDataProviderEf<TDbContext>> logger)
    : IInboxCleanupDataProvider
    where TDbContext : DbContext
{
    public async Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        const int limit = 1000;

        var stamp = DateTime.UtcNow.Subtract(olderThan);
        var total = 0;
        var cont = true;

        logger.LogDebug("Cleaning up inbox messages older than {Timestamp} with status {Status}", stamp, InboxMessageStatus.Succeeded);

        while (cont)
        {
            // Удаляем только Succeeded. DeadLettered остаётся: это сообщения, требующие ручного разбора,
            // и молча стирать их означало бы прятать инциденты.
            var affected = await dbContext
                .Set<InboxEnvelope>()
                .Where(x => x.Status == InboxMessageStatus.Succeeded && x.CreatedUtc < stamp)
                .OrderBy(x => x.CreatedUtc)
                .Take(limit)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            total += affected;
            cont = affected > 0;
        }

        return total;
    }
}
