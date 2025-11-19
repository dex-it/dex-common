using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.AspNetScheduler.Interfaces;
using Dex.Cap.OnceExecutor.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.OnceExecutor.AspNetScheduler;

internal sealed class OnceExecutorCleanerHandler(
    IOnceExecutorCleanupDataProvider onceExecutorDataProvider,
    ILogger<OnceExecutorCleanerHandler> logger) : IOnceExecutorCleanerHandler
{
    public async Task Execute(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        logger.LogDebug("Performing cleanup");

        var removedMessages = await onceExecutorDataProvider.Cleanup(olderThan, cancellationToken).ConfigureAwait(false);

        if (removedMessages > 0)
            logger.LogInformation("Cleanup finished. Transactions removed: {Count}", removedMessages);
        else
            logger.LogInformation("Cleanup finished. No transactions to remove");
    }
}