using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

public class HostileDiscriminatorCommandHandler : IInboxMessageHandler<HostileDiscriminatorCommand>
{
    public Task Process(HostileDiscriminatorCommand message, CancellationToken cancellationToken) => Task.CompletedTask;
}
