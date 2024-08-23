using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// Сервис управления настройками.
/// </summary>
public interface IAuditServerSettingsService
{
    /// <summary>
    /// Добавить или обновить настройки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="severityLevel">Уровень критичности события аудита.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddOrUpdateSettingsAsync(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить настройки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task DeleteSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}