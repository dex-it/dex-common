using System;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox;

internal sealed class InboxEnvelopFactory(IInboxSerializer serializer, IInboxTypeDiscriminatorProvider discriminatorProvider)
    : IInboxEnvelopFactory
{
    public InboxEnvelope CreateEnvelop<T>(T message, InboxMessageIdentity identity, TimeSpan? lockTimeout)
        where T : class, IInboxMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var discriminator = T.InboxTypeId;

        if (!discriminatorProvider.CurrentDomainInboxMessageTypes.ContainsKey(discriminator))
        {
            throw new DiscriminatorResolveException(
                $"Сообщение инбокса с дискриминатором '{discriminator}' не найдено среди загруженных типов данного сервиса");
        }

        var messageType = message.GetType();
        var content = serializer.Serialize(messageType, message);

        return new InboxEnvelope(Guid.NewGuid(), identity.MessageId, identity.ConsumerId, discriminator, content, lockTimeout);
    }
}
