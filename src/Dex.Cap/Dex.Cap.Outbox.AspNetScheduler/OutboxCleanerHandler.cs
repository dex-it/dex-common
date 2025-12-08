using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.AspNetScheduler;

internal sealed class OutboxCleanerHandler(IOutboxCleanupDataProvider outboxDataProvider, ILogger<OutboxCleanerHandler> logger) : IOutboxCleanerHandler
{
    public async Task Execute(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        logger.LogDebug("Performing cleanup");

        var removedMessages = await outboxDataProvider.Cleanup(olderThan, cancellationToken).ConfigureAwait(false);
        if (removedMessages > 0)
        {
            logger.LogInformation("Cleanup finished. Messages removed: {Count}", removedMessages);
        }
        else
        {
            logger.LogInformation("Cleanup finished. No messages to remove");
        }
    }
}