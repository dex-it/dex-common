using System.Text.Json;
using Dex.Audit.Client.Interfaces;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.Audit.ServerSample.Repositories;

public class AuditSettingsRepository(IDistributedCache distributedCache) : IAuditSettingsRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var eventBytes = await distributedCache.GetAsync(eventType, cancellationToken);
        if (eventBytes == null) return null;
        var auditSettings = JsonSerializer.Deserialize<AuditSettings>(eventBytes);
        return auditSettings;
    }

    public async Task AddAsync(string settingEventType, AuditSettings settings, TimeSpan refreshInterval,
        CancellationToken cancellationToken = default)
    {
        await distributedCache.SetStringAsync(settingEventType, JsonSerializer.Serialize(settings), cancellationToken);
    }
}