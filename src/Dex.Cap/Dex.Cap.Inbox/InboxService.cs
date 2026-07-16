using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox;

internal sealed class InboxService(IInboxDataProvider inboxDataProvider, IInboxEnvelopFactory envelopFactory) : IInboxService
{
    public Task<InboxEnqueueStatus> EnqueueAsync<T>(
        T message,
        InboxMessageIdentity identity,
        TimeSpan? lockTimeout,
        CancellationToken cancellationToken)
        where T : class, IInboxMessage
    {
        var inboxEnvelope = envelopFactory.CreateEnvelop(message, identity, lockTimeout);

        return inboxDataProvider.Add(inboxEnvelope, cancellationToken);
    }
}