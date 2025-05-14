using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1711

namespace Dex.Events.Distributed.OutboxExtensions
{
    public sealed class OutboxDistributedEventHandler<TBus> : IOutboxMessageHandler<OutboxDistributedEventMessage<TBus>>
        where TBus : IBus
    {
        private readonly TBus _bus;
        private readonly IOutboxTypeDiscriminator _discriminator;

        public OutboxDistributedEventHandler(TBus bus, IOutboxTypeDiscriminator discriminator)
        {
            _bus = bus;
            _discriminator = discriminator;
        }

        public Task Process(OutboxDistributedEventMessage<TBus> message,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            var messageType = _discriminator.ResolveType(message.EventParamsType);
            var eventParams = JsonSerializer.Deserialize(message.EventParams, messageType);

            // The Publish(T) and Publish(object) work differently:
            // - in the first method, the type is defined by typeof(T)
            // - in the second method, the type is defined by GetType()
            return _bus.Publish(eventParams!, cancellationToken);
        }
    }
}