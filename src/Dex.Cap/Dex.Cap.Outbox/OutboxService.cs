using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox;

internal sealed class OutboxService(IOutboxDataProvider outboxDataProvider, IOutboxSerializer serializer) : IOutboxService
{
    public Guid CorrelationId { get; } = Guid.NewGuid();

    public async Task<Guid> EnqueueAsync<T>(T message, Guid? correlationId, DateTime? startAtUtc, TimeSpan? lockTimeout, CancellationToken cToken)
        where T : class, IOutboxMessage
    {
        var messageType = message.GetType();
        var envelopeId = Guid.NewGuid();

        var msgBody = serializer.Serialize(messageType, message);
        var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId ?? CorrelationId, T.OutboxMessageType, msgBody, startAtUtc, lockTimeout);

        await outboxDataProvider
            .Add(outboxEnvelope, cToken)
            .ConfigureAwait(false);

        return envelopeId;
    }

    public Task<bool> IsOperationExistsAsync(Guid? correlationId, CancellationToken cToken)
    {
        return outboxDataProvider.IsExists(correlationId ?? CorrelationId, cToken);
    }
}