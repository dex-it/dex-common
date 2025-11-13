using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события
/// </summary>
/// <typeparam name="TMessage">Объект события</typeparam>
public class PublisherOutboxHandler<TMessage>(IPublishEndpoint publishEndpoint) : IOutboxMessageHandler<TMessage> where TMessage : class, IOutboxMessage, new()
{
    /// <summary>
    /// Является ли хендлер автопаблишером.
    /// <br/>
    /// Некоторые сообщения могут требовать явную реализацию отдельного хендлера, если для IOutboxMessage указать:
    /// <code>
    /// AllowAutoPublishing = false;
    /// </code>
    /// </summary>
    public bool IsAutoPublisher => true;

    /// <inheritdoc />
    public Task Process(TMessage message, CancellationToken cancellationToken) => publishEndpoint.Publish(message, cancellationToken);
}