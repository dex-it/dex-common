using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.Models;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxServiceExtensions
    {
        public static async Task RaiseDistributedEventAsync(this IOutboxService<object?> outboxService, Guid correlationId,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
            => await outboxService.RaiseDistributedEventAsync<IBus>(correlationId, outboxMessage, cancellationToken).ConfigureAwait(false);

        public static async Task RaiseDistributedEventAsync<TBus>(this IOutboxService<object?> outboxService, Guid correlationId,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            if (outboxService == null) throw new ArgumentNullException(nameof(outboxService));

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage);
            await outboxService.EnqueueAsync(correlationId, eventMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}