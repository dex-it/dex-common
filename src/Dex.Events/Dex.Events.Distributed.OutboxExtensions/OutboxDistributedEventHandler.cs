using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

#pragma warning disable CA1711

namespace Dex.Events.Distributed.OutboxExtensions;

public sealed class OutboxDistributedEventHandler(IBus bus, IOutboxTypeDiscriminatorProvider discriminatorProvider)
    : IOutboxMessageHandler<OutboxDistributedEventMessage>
{
    public Task Process(OutboxDistributedEventMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = discriminatorProvider.CurrentDomainOutboxMessageTypes[message.EventParamsType];
        var eventParams = JsonSerializer.Deserialize(message.EventParams, messageType);

        // The Publish(T) and Publish(object) work differently:
        // - in the first method, the type is defined by typeof(T)
        // - in the second method, the type is defined by GetType()
        return bus.Publish(eventParams!, cancellationToken);
    }
}