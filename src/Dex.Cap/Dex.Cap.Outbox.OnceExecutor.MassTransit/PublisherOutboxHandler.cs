using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события
/// </summary>
/// <typeparam name="TMessage">Объект события</typeparam>
public class PublisherOutboxHandler<TMessage>(IPublishEndpoint publishEndpoint) : IOutboxMessageHandler<TMessage> where TMessage : class, IOutboxMessage
{
    public static bool IsAutoPublisher => true;

    /// <inheritdoc />
    public Task Process(TMessage message, CancellationToken cancellationToken) => publishEndpoint.Publish(message, cancellationToken);
}