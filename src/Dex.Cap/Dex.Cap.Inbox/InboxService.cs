using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox;

internal sealed class InboxService(
    IInboxDataProvider inboxDataProvider,
    IInboxEnvelopFactory envelopFactory,
    IOptions<InboxOptions> options) : IInboxService
{
    private readonly InboxOptions _options = options.Value;

    public Task<InboxEnqueueStatus> EnqueueAsync<T>(
        T message,
        InboxMessageIdentity identity,
        TimeSpan? lockTimeout,
        CancellationToken cancellationToken)
        where T : class, IInboxMessage
    {
        identity.EnsureInitialized(nameof(identity));

        var inboxEnvelope = envelopFactory.CreateEnvelop(message, identity, lockTimeout);

        EnsureContentWithinLimit(inboxEnvelope);

        return inboxDataProvider.Add(inboxEnvelope, cancellationToken);
    }

    private void EnsureContentWithinLimit(InboxEnvelope envelope)
    {
        var contentLength = Encoding.UTF8.GetByteCount(envelope.Content);
        if (contentLength > _options.MaxContentLength)
        {
            throw new InboxContentTooLargeException(envelope.MessageType, contentLength, _options.MaxContentLength);
        }
    }
}