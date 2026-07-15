using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox;

/// <summary>
/// Вызывает обработчик через контракт <see cref="IInboxMessageHandler{TMessage}"/>, а не поиском метода.
/// </summary>
/// <remarks>
/// Закрытый generic-тип создаётся один раз на тип сообщения и кэшируется, поэтому рефлексия ограничена
/// созданием инстанса и не участвует в самом вызове.
/// </remarks>
internal sealed class InboxMessageInvoker<TMessage> : IInboxMessageInvoker
    where TMessage : IInboxMessage
{
    public Task InvokeAsync(object handler, object message, CancellationToken cancellationToken)
    {
        // Приведение безопасно: обработчик получен из DI именно по этому закрытому интерфейсу,
        // а сообщение десериализовано в тип, из которого этот интерфейс и построен.
        return ((IInboxMessageHandler<TMessage>)handler).Process((TMessage)message, cancellationToken);
    }
}
