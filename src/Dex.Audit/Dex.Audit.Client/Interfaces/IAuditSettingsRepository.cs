using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Interfaces;

public interface IAuditSettingsRepository
{
    Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default);

    Task AddAsync(string settingEventType, AuditSettings setting, TimeSpan refreshInterval);
}