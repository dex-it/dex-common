using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события
/// </summary>
/// <typeparam name="T">Объект события</typeparam>
public class PublisherOutboxHandler<T> : IOutboxMessageHandler<T> where T : IConsumer, IOutboxMessage
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PublisherOutboxHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    /// <inheritdoc />
    public Task ProcessMessage(T message, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
    {
        return ProcessMessage((T)outbox, cancellationToken);
    }
}