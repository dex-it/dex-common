using System;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox;

internal sealed class InboxMessageHandlerFactory(IServiceProvider serviceProvider) : IInboxMessageHandlerFactory
{
    public object GetMessageHandler(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var handlerType = typeof(IInboxMessageHandler<>).MakeGenericType(messageType);
        var handler = serviceProvider.GetService(handlerType);

        return handler ?? throw new InboxException($"Can't resolve message handler for '{handlerType}'");
    }
}
