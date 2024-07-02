using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using MassTransit;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Реализация интерфейса <see cref="IAuditOutputProvider"/>, которая осуществляет публикацию аудиторских событий через RabbitMQ
/// </summary>
internal sealed class AuditOutputRabbitProvider : IAuditOutputProvider
{
    private readonly ISendEndpointProvider _sendEndpoint;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AuditOutputRabbitProvider"/>
    /// </summary>
    /// <param name="sendEndpoint">Конечная точка для публикации сообщений</param>
    public AuditOutputRabbitProvider(ISendEndpointProvider sendEndpoint)
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
