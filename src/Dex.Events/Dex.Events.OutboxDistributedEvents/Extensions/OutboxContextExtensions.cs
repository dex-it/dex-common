using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using DistributedEvents;
using MassTransit;

namespace Dex.Events.OutboxDistributedEvents.Extensions
{
    public static class OutboxContextExtensions
    {
        public static async Task RaiseDistributedEventAsync(this IOutboxContext<object?, object?> outboxContext,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
        {
            await outboxContext.RaiseDistributedEventAsync<IBus>(outboxMessage, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RaiseDistributedEventAsync<TBus>(this IOutboxContext<object?, object?> outboxContext,
            DistributedBaseEventParams outboxMessage, CancellationToken cancellationToken = default)
            where TBus : IBus
        {
            if (outboxContext == null) throw new ArgumentNullException(nameof(outboxContext));
            if (outboxMessage == null) throw new ArgumentNullException(nameof(outboxMessage));

            var messageType = outboxMessage.GetType();

            await outboxContext.EnqueueMessageAsync(
                new OutboxDistributedEventMessage<TBus>(JsonSerializer.Serialize(outboxMessage, messageType), messageType.AssemblyQualifiedName!),
                cancellationToken).ConfigureAwait(false);
        }
    }
}