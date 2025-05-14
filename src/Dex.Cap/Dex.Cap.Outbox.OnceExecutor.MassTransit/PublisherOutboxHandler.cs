using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события
/// </summary>
/// <typeparam name="TMessage">Объект события</typeparam>
public class PublisherOutboxHandler<TMessage> : IOutboxMessageHandler<TMessage>
    where TMessage : class
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PublisherOutboxHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    /// <inheritdoc />
    public Task Process(TMessage message, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }
}