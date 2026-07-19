using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox;

internal sealed class OutboxService(
    IOutboxDataProvider outboxDataProvider,
    IOutboxEnvelopFactory envelopFactory,
    IOptions<OutboxOptions> options) : IOutboxService
{
    private readonly OutboxOptions _options = options.Value;

    public Guid CorrelationId { get; } = Guid.NewGuid();

    public async Task<Guid> EnqueueAsync<T>(T message, Guid? correlationId, DateTime? startAtUtc, TimeSpan? lockTimeout, CancellationToken cancellationToken)
        where T : class, IOutboxMessage
    {
        var outboxEnvelope = envelopFactory.CreateEnvelop(message, correlationId ?? CorrelationId, startAtUtc, lockTimeout);

        EnsureContentWithinLimit(outboxEnvelope);

        await outboxDataProvider
            .Add(outboxEnvelope, cancellationToken)
            .ConfigureAwait(false);

        return outboxEnvelope.Id;
    }

    private void EnsureContentWithinLimit(OutboxEnvelope envelope)
    {
        var contentLength = Encoding.UTF8.GetByteCount(envelope.Content);
        if (contentLength > _options.MaxContentLength)
        {
            throw new OutboxContentTooLargeException(envelope.MessageType, contentLength, _options.MaxContentLength);
        }
    }

    public Task<bool> IsOperationExistsAsync(Guid? correlationId, CancellationToken cToken)
    {
        return outboxDataProvider.IsExists(correlationId ?? CorrelationId, cToken);
    }
}