using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.MassTransit.Outbox.Extensions.Outbox;

/// <summary>
/// Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события
/// </summary>
/// <typeparam name="T">Объект события</typeparam>
public class GenericMassTransitPublisher<T> : IOutboxMessageHandler<T> where T : IConsumer, IOutboxMessage
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GenericMassTransitPublisher(IPublishEndpoint publishEndpoint)
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