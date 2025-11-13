using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.OutboxExtensions;

namespace Dex.Events.Distributed.Tests.Handlers;

public sealed class TestOutboxDistributedEventHandler : IOutboxMessageHandler<OutboxDistributedEventMessage>
{
    public static event EventHandler<OutboxDistributedEventMessage> OnProcess;

    public Task Process(OutboxDistributedEventMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        Console.WriteLine($"{nameof(TestOutboxDistributedEventHandler)} - Processed command at {DateTime.Now}, Args: {message.EventParams}, {message.EventParamsType}");
        OnProcess?.Invoke(null, message);

        return Task.CompletedTask;
    }
}