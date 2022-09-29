using System;
using System.Text.Json;
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
            if (outboxMessage == null) throw new ArgumentNullException(nameof(outboxMessage));

            var messageType = outboxMessage.GetType();
            var eventParams = JsonSerializer.Serialize(outboxMessage, messageType);
            var eventMessage = new OutboxDistributedEventMessage<TBus>(eventParams, messageType.AssemblyQualifiedName!);

            await outboxContext.EnqueueMessageAsync(eventMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}