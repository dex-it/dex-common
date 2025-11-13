using System;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox;

internal sealed class OutboxMessageHandlerFactory(IServiceProvider serviceProvider) : IOutboxMessageHandlerFactory
{
    public object GetMessageHandler(object outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        var messageType = outboxMessage.GetType();
        var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(messageType);
        var handler = serviceProvider.GetService(handlerType);

        return handler ?? throw new OutboxException($"Can't resolve message handler for '{handlerType}'");
    }
}