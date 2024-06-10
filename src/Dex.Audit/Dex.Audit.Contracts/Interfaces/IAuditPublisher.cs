using Dex.Audit.Contracts.Messages;

namespace Dex.Audit.Contracts.Interfaces;

/// <summary>
/// Определяет метод для публикации аудиторских событий
/// </summary>
public interface IAuditPublisher
{
    /// <summary>
    /// Асинхронно публикует аудиторское событие
    /// </summary>
    /// <param name="auditEvent">Аудиторское событие, которое нужно опубликовать</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task PublishEventAsync(AuditEventMessage auditEvent, CancellationToken cancellationToken = default);
}
