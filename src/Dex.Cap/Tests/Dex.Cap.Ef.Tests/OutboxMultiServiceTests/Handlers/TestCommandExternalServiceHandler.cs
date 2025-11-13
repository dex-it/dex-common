using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestCommandExternalServiceHandler : IOutboxMessageHandler<TestOutboxExternalServiceCommand>
{
    public static event EventHandler<TestOutboxExternalServiceCommand>? OnProcess;

    public Task Process(TestOutboxExternalServiceCommand message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"TestCommandUnregisteredHandler - Processed command at {DateTime.Now}, Args: {message.Args}");
        OnProcess?.Invoke(this, message);
        return Task.CompletedTask;
    }
}