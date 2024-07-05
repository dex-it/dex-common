using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using MassTransit;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Реализация интерфейса <see cref="IAuditOutputProvider"/>, которая осуществляет публикацию аудиторских событий через RabbitMQ
/// </summary>
/// <param name="sendEndpoint">Конечная точка для публикации сообщений</param>
internal sealed class AuditOutputRabbitProvider(ISendEndpointProvider sendEndpoint) : IAuditOutputProvider
{
    /// <summary>
    /// Публикует аудиторское событие через RabbitMQ
    /// </summary>
    /// <param name="auditEvent">Аудиторское событие, которое нужно опубликовать</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task PublishEventAsync(AuditEventMessage auditEvent, CancellationToken cancellationToken = default)
    {
        await sendEndpoint.Send(auditEvent, cancellationToken).ConfigureAwait(false);
    }
}
