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
        public static async Task RaiseDistributedEventAsync<TDbContext, TState, TBus, TMessage>(this IOutboxContext<TDbContext, TState> outboxContext,
            TMessage outboxMessage, CancellationToken cancellationToken)
            where TBus : IBus
            where TMessage : DistributedBaseEventParams
        {
            if (outboxContext == null) throw new ArgumentNullException(nameof(outboxContext));
            if (outboxMessage == null) throw new ArgumentNullException(nameof(outboxMessage));

            await outboxContext.EnqueueMessageAsync(
                new OutboxDistributedEventMessage<TBus>(JsonSerializer.Serialize(outboxMessage), outboxMessage.GetType().AssemblyQualifiedName!),
                cancellationToken).ConfigureAwait(false);
        }
    }
}