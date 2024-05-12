using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Cap.Outbox.Models;
using Dex.Events.Distributed.Models;
using MassTransit;

namespace Dex.Events.Distributed.OutboxExtensions
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public sealed class OutboxDistributedEventMessage<TBus> : BaseOutboxMessage where TBus : IBus
    {
        public string EventParams { get; }
        public string EventParamsType { get; }

        [JsonConstructor]
        public OutboxDistributedEventMessage(string eventParams, string eventParamsType)
        {
            EventParams = eventParams;
            EventParamsType = eventParamsType;
        }

        public OutboxDistributedEventMessage(DistributedBaseEventParams outboxMessage)
        {
            ArgumentNullException.ThrowIfNull(outboxMessage);

            var messageType = outboxMessage.GetType();
            var eventParams = JsonSerializer.Serialize(outboxMessage, messageType);
            EventParams = eventParams;
            EventParamsType = messageType.AssemblyQualifiedName!;
        }
    }
}