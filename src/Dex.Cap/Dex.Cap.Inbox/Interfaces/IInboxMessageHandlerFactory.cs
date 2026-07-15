using System;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxMessageHandlerFactory
{
    /// <summary>
    /// Получить обработчик для сообщения указанного типа.
    /// </summary>
    object GetMessageHandler(Type messageType);
}
