using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

public class UrnDiscriminatorCommandHandler : IInboxMessageHandler<UrnDiscriminatorCommand>
{
    public Task Process(UrnDiscriminatorCommand message, CancellationToken cancellationToken) => Task.CompletedTask;
}
