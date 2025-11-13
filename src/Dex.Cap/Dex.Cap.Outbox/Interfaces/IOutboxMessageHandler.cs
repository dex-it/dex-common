using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxMessageHandler<in TMessage>
    where TMessage : class, IOutboxMessage, new()
{
    /// <summary>
    /// Предназначен ли данный хендлер для автоматической публикации сообщений любых типов
    /// <br/>
    /// IOutboxMessage может отказаться от автоматической публикации и требовать явной реализации отдельного хендлера с помощью
    /// <code>IOutboxMessage.AllowAutoPublishing = false</code>
    /// </summary>
    bool IsAutoPublisher => false;

    Task Process(TMessage message, CancellationToken cancellationToken);
}