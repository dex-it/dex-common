using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using MassTransit;

namespace Dex.Events.Distributed.OutboxExtensions
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public sealed class OutboxDistributedEventMessage<TBus> : BaseOutboxMessage
        where TBus : IBus
    {
        public string EventParams { get; }
        public string EventParamsType { get; }

        [JsonConstructor]
        public OutboxDistributedEventMessage(string eventParams, string eventParamsType)
        {
            EventParams = eventParams;
            EventParamsType = eventParamsType;
        }

        public OutboxDistributedEventMessage(
            IDistributedEventParams? outboxMessage,
            IOutboxTypeDiscriminator discriminator)
        {
            ArgumentNullException.ThrowIfNull(outboxMessage);
            ArgumentNullException.ThrowIfNull(discriminator);

            var messageType = outboxMessage.GetType();
            var eventParams = JsonSerializer.Serialize(outboxMessage, messageType);

            EventParams = eventParams;
            EventParamsType = discriminator.ResolveDiscriminator(messageType);
        }
    }
}