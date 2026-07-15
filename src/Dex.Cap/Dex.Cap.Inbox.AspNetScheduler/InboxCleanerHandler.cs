using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Interfaces;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox.AspNetScheduler;

internal sealed class InboxCleanerHandler(IInboxCleanupDataProvider inboxDataProvider, ILogger<InboxCleanerHandler> logger) : IInboxCleanerHandler
{
    public async Task Execute(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        logger.LogDebug("Performing cleanup");

        var removedMessages = await inboxDataProvider.Cleanup(olderThan, cancellationToken).ConfigureAwait(false);
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
