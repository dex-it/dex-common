using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Интерфейс 
/// </summary>
public interface IAuditSettingsService
{
    /// <summary>
    /// Получить или получить и обновить настройки
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}