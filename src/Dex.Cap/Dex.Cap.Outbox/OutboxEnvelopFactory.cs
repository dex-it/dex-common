using System;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox;

internal sealed class OutboxEnvelopFactory(IOutboxSerializer serializer, IOutboxTypeDiscriminatorProvider discriminatorProvider) : IOutboxEnvelopFactory
{
    public OutboxEnvelope CreateEnvelop<T>(T message, Guid correlationId, DateTime? startAtUtc, TimeSpan? lockTimeout) where T : class, IOutboxMessage
    {
        var supportedDiscriminators = discriminatorProvider.CurrentDomainOutboxMessageTypes;

        if (supportedDiscriminators.ContainsKey(T.OutboxTypeId) is false)
            throw new DiscriminatorResolveException($"Сообщение {T.OutboxTypeId} не найдено в данном сервисе");

        var messageType = message.GetType();
        var envelopeId = Guid.NewGuid();

        var msgBody = serializer.Serialize(messageType, message);
        var outboxEnvelope = new OutboxEnvelope(envelopeId, correlationId, T.OutboxTypeId, msgBody, startAtUtc, lockTimeout);

        return outboxEnvelope;
    }
}