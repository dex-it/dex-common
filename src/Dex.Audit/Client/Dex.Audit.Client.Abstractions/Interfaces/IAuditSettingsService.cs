using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Интерфейс сервиса настроек аудита.
/// </summary>
public interface IAuditSettingsService
{
    /// <summary>
    /// Получить или получить и обновить настройки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Настройка аудита.</returns>
    Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}