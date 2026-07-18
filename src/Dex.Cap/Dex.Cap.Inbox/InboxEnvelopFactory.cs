using System;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox;

internal sealed class InboxEnvelopFactory(IInboxSerializer serializer, IInboxTypeDiscriminatorProvider discriminatorProvider)
    : IInboxEnvelopFactory
{
    /// <inheritdoc />
    /// <remarks>
    /// Тело сериализуется по typeof(T), а не по message.GetType(): дискриминатор берётся из статического
    /// T, и рассогласование дало бы тело наследника под дискриминатором базового типа. Такое сообщение
    /// прочиталось бы как базовый тип, молча потеряв поля наследника.
    /// </remarks>
    public InboxEnvelope CreateEnvelop<T>(T message, InboxMessageIdentity identity, TimeSpan? lockTimeout)
        where T : class, IInboxMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        var discriminator = T.InboxTypeId;

        if (!discriminatorProvider.CurrentDomainInboxMessageTypes.ContainsKey(discriminator))
        {
            throw new DiscriminatorResolveException(
                $"Inbox message type '{typeof(T).FullName}' with discriminator '{discriminator}' is not found " +
                "among the loaded types of this service. The assembly declaring the message type must be loaded.");
        }

        var content = serializer.Serialize(typeof(T), message);

        return new InboxEnvelope(Guid.NewGuid(), identity.MessageId, identity.ConsumerId, discriminator, content, lockTimeout);
    }
}