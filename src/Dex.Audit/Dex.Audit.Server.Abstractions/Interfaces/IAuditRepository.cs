using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// Репозиторий постоянного хранилища.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Добавить событие.
    /// </summary>
    /// <param name="auditEvents">Событие.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddAuditEventsRangeAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все настройки.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
}