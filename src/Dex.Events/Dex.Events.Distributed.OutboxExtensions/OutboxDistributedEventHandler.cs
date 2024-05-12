using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1711

namespace Dex.Events.Distributed.OutboxExtensions
{
    public sealed class OutboxDistributedEventHandler<TBus>(TBus bus) : IOutboxMessageHandler<OutboxDistributedEventMessage<TBus>>
        where TBus : IBus
    {
        private readonly TBus _bus = bus;

        public async Task ProcessMessage(OutboxDistributedEventMessage<TBus> message, CancellationToken cancellationToken)
        {
            // The Publish(T) and Publish(object) work differently:
            // - in the first method, the type is defined by typeof(T)
            // - in the second method, the type is defined by GetType()

            ArgumentNullException.ThrowIfNull(message);

            var eventParams = JsonSerializer.Deserialize(message.EventParams, Type.GetType(message.EventParamsType)!);
            await _bus.Publish(eventParams!, cancellationToken).ConfigureAwait(false);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((OutboxDistributedEventMessage<TBus>)outbox, cancellationToken);
        }
    }
}