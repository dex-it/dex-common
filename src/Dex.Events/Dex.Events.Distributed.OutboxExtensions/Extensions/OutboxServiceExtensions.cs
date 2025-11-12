using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxServiceExtensions
    {
        public static Task<Guid> EnqueueEventAsync(
            this IOutboxService outboxService,
            IOutboxMessage outboxMessage,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(outboxService);

            var eventMessage = new OutboxDistributedEventMessage(outboxMessage);
            return outboxService.EnqueueAsync(eventMessage, correlationId, cancellationToken: cancellationToken);
        }
    }
}