using Dex.Audit.Client.Interfaces;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.ClientSample.Repositories;

public class AuditSettingsRepository : IAuditSettingsRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return new AuditSettings();
    }

    public Task AddAsync(string settingEventType, AuditSettings setting, TimeSpan refreshInterval,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}