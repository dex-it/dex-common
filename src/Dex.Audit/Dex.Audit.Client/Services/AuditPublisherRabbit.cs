using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using MassTransit;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Реализация интерфейса <see cref="IAuditPublisher"/>, которая осуществляет публикацию аудиторских событий через RabbitMQ
/// </summary>
internal sealed class AuditPublisherRabbit : IAuditPublisher
{
    private readonly ISendEndpointProvider _sendEndpoint;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AuditPublisherRabbit"/>
    /// </summary>
    /// <param name="sendEndpoint">Конечная точка для публикации сообщений</param>
    public AuditPublisherRabbit(ISendEndpointProvider sendEndpoint)
    {
        _sendEndpoint = sendEndpoint;
    }

    /// <summary>
    /// Публикует аудиторское событие через RabbitMQ
    /// </summary>
    /// <param name="auditEvent">Аудиторское событие, которое нужно опубликовать</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task PublishEventAsync(AuditEventMessage auditEvent, CancellationToken cancellationToken = default)
    {
        await _sendEndpoint.Send(auditEvent, cancellationToken);
    }
}
