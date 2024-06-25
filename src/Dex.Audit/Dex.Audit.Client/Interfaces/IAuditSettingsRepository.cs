using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Interfaces;

/// <summary>
/// Репозиторий настроек
/// </summary>
public interface IAuditSettingsRepository
{
    Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default);

    Task AddAsync(string settingEventType, AuditSettings setting, TimeSpan refreshInterval);
}