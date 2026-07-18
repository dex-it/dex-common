using System;
using System.Collections.Concurrent;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox;

internal sealed class InboxMessageHandlerFactory(IServiceProvider serviceProvider) : IInboxMessageHandlerFactory
{
    /// <remarks>
    /// Инвокеры не зависят от scope, поэтому переживают его: словарь статический, чтобы не строить
    /// закрытый generic-тип на каждое сообщение.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, IInboxMessageInvoker> Invokers = new();

    public object GetMessageHandler(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var handlerType = typeof(IInboxMessageHandler<>).MakeGenericType(messageType);
        var handler = serviceProvider.GetService(handlerType);

        return handler ?? throw new InboxException($"Can't resolve message handler for '{handlerType}'");
    }

    public IInboxMessageInvoker GetInvoker(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return Invokers.GetOrAdd(
            messageType,
            static type => (IInboxMessageInvoker)Activator.CreateInstance(typeof(InboxMessageInvoker<>).MakeGenericType(type))!);
    }
}