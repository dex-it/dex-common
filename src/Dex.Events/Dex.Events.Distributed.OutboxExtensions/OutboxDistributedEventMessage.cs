using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Events.Distributed.OutboxExtensions
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public sealed record OutboxDistributedEventMessage<TBus>(string EventParams, string EventParamsType) : IOutboxMessage where TBus : IBus;
}