using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox;

internal sealed class InboxService(
    IInboxDataProvider inboxDataProvider,
    IInboxEnvelopFactory envelopFactory,
    IOptions<InboxOptions> options,
    ILogger<InboxService> logger) : IInboxService
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
        var contentLengthBytes = Encoding.UTF8.GetByteCount(envelope.Content);
        if (contentLengthBytes <= _options.MaxContentLengthBytes)
        {
            return;
        }

        // Логируем именно здесь, а не оставляем это вызывающему: строка в таблицу не попадает, поэтому отказ
        // невидим для всей наблюдаемости инбокса (статусы, счётчики, health check), а тело пришло от внешнего
        // источника, и «источник начал слать сообщения сверх предела» это сигнал оператору, а не только
        // вызывающему. Само тело в лог не пишем.
        logger.LogWarning(
            "Inbox message {MessageType} rejected: body is {ContentLengthBytes} bytes, configured limit is {MaxContentLengthBytes}",
            envelope.MessageType,
            contentLengthBytes,
            _options.MaxContentLengthBytes);

        throw new InboxContentTooLargeException(envelope.MessageType, contentLengthBytes, _options.MaxContentLengthBytes);
    }
}