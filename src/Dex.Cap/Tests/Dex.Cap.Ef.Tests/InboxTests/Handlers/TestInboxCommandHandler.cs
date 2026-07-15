using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

public class TestInboxCommandHandler : IInboxMessageHandler<TestInboxCommand>
{
    public static event EventHandler<TestInboxCommand>? OnProcess;

    public Task Process(TestInboxCommand message, CancellationToken cancellationToken)
    {
        OnProcess?.Invoke(this, message);
        return Task.CompletedTask;
    }
}
