using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;

public class TestCommandExternalServiceHandler : IOutboxMessageHandler<TestOutboxExternalServiceCommand>
{
    public static event EventHandler<TestOutboxExternalServiceCommand> OnProcess = null!;

    public Task ProcessMessage(TestOutboxExternalServiceCommand message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"TestCommandUnregisteredHandler - Processed command at {DateTime.Now}, Args: {message.Args}");
        OnProcess.Invoke(this, message);
        return Task.CompletedTask;
    }

    public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
    {
        return ProcessMessage((TestOutboxExternalServiceCommand)outbox, cancellationToken);
    }
}