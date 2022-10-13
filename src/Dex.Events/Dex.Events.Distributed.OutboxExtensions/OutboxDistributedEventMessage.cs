using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.Models;
using MassTransit;

namespace Dex.Events.Distributed.OutboxExtensions
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public sealed record OutboxDistributedEventMessage<TBus> : IOutboxMessage where TBus : IBus
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
            if (outboxMessage == null) throw new ArgumentNullException(nameof(outboxMessage));

            var messageType = outboxMessage.GetType();
            var eventParams = JsonSerializer.Serialize(outboxMessage, messageType);
            EventParams = eventParams;
            EventParamsType = messageType.AssemblyQualifiedName!;
        }
    }
}