using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// Репозиторий постоянного хранилища настроек.
/// </summary>
public interface IAuditSettingsRepository
{
    /// <summary>
    /// Добавить или обновить настройки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="severityLevel">Уровень критичности события аудита.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddOrUpdateSettings(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить настройки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task DeleteSettings(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все настройки.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
}