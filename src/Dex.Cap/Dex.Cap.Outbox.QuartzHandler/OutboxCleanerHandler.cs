using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.AspNetScheduler;

internal sealed class OutboxCleanerHandler : IOutboxCleanerHandler
{
    private readonly IOutboxCleanupDataProvider _outboxDataProvider;
    private readonly ILogger _logger;

    public OutboxCleanerHandler(IOutboxCleanupDataProvider outboxDataProvider, ILogger<OutboxCleanerHandler> logger)
    {
        _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing cleanup");

        var removedMessages = await _outboxDataProvider.Cleanup(olderThan, cancellationToken).ConfigureAwait(false);
        if (removedMessages > 0)
        {
            _logger.LogInformation("Cleanup finished. Messages removed: {Count}", removedMessages);
        }
        else
        {
            _logger.LogInformation("Cleanup finished. No messages to remove");
        }
    }
}