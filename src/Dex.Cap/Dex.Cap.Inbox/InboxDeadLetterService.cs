using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox;

internal sealed class InboxDeadLetterService(IInboxDataProvider inboxDataProvider) : IInboxDeadLetterService
{
    public async Task<bool> RequeueAsync(InboxMessageIdentity identity, CancellationToken cancellationToken = default)
    {
        identity.EnsureInitialized(nameof(identity));

        var requeued = await inboxDataProvider.RequeueDeadLetteredAsync(identity, cancellationToken).ConfigureAwait(false);

        return requeued is not 0;
    }

    public Task<int> RequeueAllAsync(CancellationToken cancellationToken = default) =>
        inboxDataProvider.RequeueAllDeadLetteredAsync(cancellationToken);
}