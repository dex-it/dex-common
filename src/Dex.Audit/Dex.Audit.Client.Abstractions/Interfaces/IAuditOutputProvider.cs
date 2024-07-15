using Dex.Audit.Client.Abstractions.Messages;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Определяет метод для публикации аудиторских событий
/// </summary>
internal interface IAuditOutputProvider
{
    /// <summary>
    /// Асинхронно публикует аудиторское событие
    /// </summary>
    /// <param name="auditEvent">Аудиторское событие, которое нужно опубликовать</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task PublishEventAsync(AuditEventMessage auditEvent, CancellationToken cancellationToken = default);
}
