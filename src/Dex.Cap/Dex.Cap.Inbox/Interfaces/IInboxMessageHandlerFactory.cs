using System;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxMessageHandlerFactory
{
    /// <summary>
    /// Получить обработчик для сообщения указанного типа.
    /// </summary>
    object GetMessageHandler(Type messageType);

    /// <summary>
    /// Получить типизированный вызов обработчика для указанного типа сообщения.
    /// </summary>
    IInboxMessageInvoker GetInvoker(Type messageType);
}
