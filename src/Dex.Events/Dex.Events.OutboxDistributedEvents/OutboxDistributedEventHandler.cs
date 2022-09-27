using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Events.OutboxDistributedEvents
{
    public sealed class OutboxDistributedEventHandler<TBus> : IOutboxMessageHandler<OutboxDistributedEventMessage<TBus>> where TBus : IBus
    {
        private readonly TBus _bus;

        public OutboxDistributedEventHandler(TBus bus)
        {
            _bus = bus;
        }

        public async Task ProcessMessage(OutboxDistributedEventMessage<TBus> message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var eventParams = JsonSerializer.Deserialize(message.EventParams, Type.GetType(message.EventParamsType)!);
            await _bus.Publish(eventParams!, cancellationToken).ConfigureAwait(false);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((OutboxDistributedEventMessage<TBus>)outbox, cancellationToken);
        }
    }
}