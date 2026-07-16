using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

public class TestErrorInboxCommandHandler : IInboxMessageHandler<TestErrorInboxCommand>
{
    public static int ProcessAttempts;

    public Task Process(TestErrorInboxCommand message, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref ProcessAttempts);
        throw new InvalidOperationException("Handler always fails");
    }
}