using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Cap.Outbox.Interfaces;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Dex.Events.Distributed.OutboxExtensions;

[SuppressMessage("ReSharper", "UnusedTypeParameter")]
public sealed class OutboxDistributedEventMessage : IOutboxMessage
{
    public string OutboxTypeId => nameof(OutboxDistributedEventMessage);

    public string EventParams { get; }

    public string EventParamsType { get; }

    [Obsolete("For serialization only")]
    public OutboxDistributedEventMessage()
    {
    }

    [JsonConstructor]
    public OutboxDistributedEventMessage(string eventParams, string eventParamsType)
    {
        EventParams = eventParams;
        EventParamsType = eventParamsType;
    }

    public OutboxDistributedEventMessage(IOutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        var messageType = outboxMessage.GetType();
        var eventParams = JsonSerializer.Serialize(outboxMessage, messageType);

        EventParams = eventParams;
        EventParamsType = outboxMessage.OutboxTypeId;
    }
}