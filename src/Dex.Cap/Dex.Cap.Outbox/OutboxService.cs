using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox;

internal sealed class OutboxService(IOutboxDataProvider outboxDataProvider, IOutboxEnvelopFactory envelopFactory) : IOutboxService
{
    public Guid CorrelationId { get; } = Guid.NewGuid();

    public async Task<Guid> EnqueueAsync<T>(T message, Guid? correlationId, DateTime? startAtUtc, TimeSpan? lockTimeout, CancellationToken cancellationToken)
        where T : class, IOutboxMessage
    {
        var outboxEnvelope = envelopFactory.CreateEnvelop(message, correlationId ?? CorrelationId, startAtUtc, lockTimeout);

        await outboxDataProvider
            .Add(outboxEnvelope, cancellationToken)
            .ConfigureAwait(false);

        return outboxEnvelope.Id;
    }

    public Task<bool> IsOperationExistsAsync(Guid? correlationId, CancellationToken cToken)
    {
        return outboxDataProvider.IsExists(correlationId ?? CorrelationId, cToken);
    }
}