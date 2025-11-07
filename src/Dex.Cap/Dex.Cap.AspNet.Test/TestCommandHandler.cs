using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.AspNet.Test;

public class TestCommandHandler(ILogger<TestCommandHandler> logger) : IOutboxMessageHandler<TestOutboxCommand>
{
    public static event EventHandler OnProcess;

    public async Task Process(TestOutboxCommand message, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);

        logger.LogInformation("TestCommandHandler - Processed command at {Now}, Args: {MessageArgs}", DateTime.Now, message.Args);

        OnProcess?.Invoke(this, EventArgs.Empty);
    }
}

public class TestOutboxCommand : IOutboxMessage
{
    public static string OutboxMessageType => "15CAD1F5-4C0D-4816-B5D1-E2340144C4AA";

    public string Args { get; init; }
}