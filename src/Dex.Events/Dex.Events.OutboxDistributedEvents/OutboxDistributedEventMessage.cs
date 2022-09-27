using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Events.OutboxDistributedEvents
{
    public sealed record OutboxDistributedEventMessage<TBus>(string EventParams, string EventParamsType) : IOutboxMessage where TBus : IBus;
}