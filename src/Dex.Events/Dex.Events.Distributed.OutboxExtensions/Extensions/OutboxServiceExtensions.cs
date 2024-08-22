using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxServiceExtensions
    {
        public static Task EnqueueEventAsync(
            this IOutboxService<object?> outboxService,
            Guid correlationId,
            IDistributedEventParams outboxMessage,
            CancellationToken cancellationToken = default)
            => outboxService.EnqueueEventAsync<IBus>(correlationId, outboxMessage, cancellationToken);

        public static async Task EnqueueEventAsync<TBus>(
            this IOutboxService<object?> outboxService,
            Guid correlationId,
            IDistributedEventParams outboxMessage,
            CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            ArgumentNullException.ThrowIfNull(outboxService);

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage, outboxService.Discriminator);
            await outboxService.EnqueueAsync(correlationId, eventMessage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}