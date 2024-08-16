using System.Text.Json;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.Audit.ClientSample.Repositories;

public class AuditCacheRepository(IDistributedCache distributedCache) : IAuditCacheRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var settingsArray = await distributedCache.GetAsync(eventType, cancellationToken);
        return settingsArray == null ? null : JsonSerializer.Deserialize<AuditSettings>(settingsArray);
    }

    public async Task AddRangeAsync(IEnumerable<AuditSettings> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            await distributedCache.SetAsync(setting.EventType, JsonSerializer.SerializeToUtf8Bytes(setting), token: cancellationToken);
        }
    }
}