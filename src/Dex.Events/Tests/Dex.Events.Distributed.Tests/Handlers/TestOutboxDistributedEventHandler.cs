using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.OutboxExtensions;
using MassTransit;

namespace Dex.Events.Distributed.Tests.Handlers
{
    public sealed class TestOutboxDistributedEventHandler<TBus> : IOutboxMessageHandler<OutboxDistributedEventMessage<TBus>>
        where TBus : IBus
    {
        public static event EventHandler<OutboxDistributedEventMessage<TBus>> OnProcess;

        public Task Process(OutboxDistributedEventMessage<TBus> message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Console.WriteLine(
                $"{nameof(TestOutboxDistributedEventHandler<TBus>)} - Processed command at {DateTime.Now}, Args: {message.EventParams}, {message.EventParamsType}");
            OnProcess?.Invoke(null, message);

            return Task.CompletedTask;
        }
    }
}