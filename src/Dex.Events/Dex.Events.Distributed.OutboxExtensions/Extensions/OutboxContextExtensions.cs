using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.Models;
using MassTransit;

#pragma warning disable CA1030

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxContextExtensions
    {
        public static async Task RaiseDistributedEventAsync(this IOutboxContext<object?, object?> outboxContext,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
            => await outboxContext.RaiseDistributedEventAsync<IBus>(outboxMessage, cancellationToken).ConfigureAwait(false);

        public static async Task RaiseDistributedEventAsync<TBus>(this IOutboxContext<object?, object?> outboxContext,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            if (outboxContext == null) throw new ArgumentNullException(nameof(outboxContext));

            var eventMessage = new OutboxDistributedEventMessage<TBus>(outboxMessage);
            await outboxContext.EnqueueAsync(eventMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}