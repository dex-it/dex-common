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
        public static Task<Guid> EnqueueEventAsync(
            this IOutboxService outboxService,
            object outboxMessage,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
            => outboxService.EnqueueEventAsync<IBus>(outboxMessage, correlationId, cancellationToken);

        public static Task<Guid> EnqueueEventAsync<TBus>(
            this IOutboxService outboxService,
            object outboxMessage,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            ArgumentNullException.ThrowIfNull(outboxService);

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage, outboxService.Discriminator);
            return outboxService.EnqueueAsync(eventMessage, correlationId, cancellationToken: cancellationToken);
        }
    }
}