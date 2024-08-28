using System.Text.Json;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.Audit.ClientSample.Infrastructure.Repositories;

public class AuditSettingsCacheRepository(IDistributedCache distributedCache)
    : IAuditSettingsCacheRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var settingsArray = await distributedCache.GetAsync(eventType, cancellationToken);
        return settingsArray == null ? null : JsonSerializer.Deserialize<AuditSettings>(settingsArray);
    }

    public async Task<IDictionary<string, AuditSettings?>> GetDictionaryAsync(IEnumerable<string> eventTypes, CancellationToken cancellationToken = default)
    {
        var enumerable = eventTypes as string[] ?? eventTypes.ToArray();

        var settingsList = new Dictionary<string, AuditSettings?>(enumerable.Length);

        foreach (var eventType in enumerable)
        {
            var settingsArray = await distributedCache.GetAsync(eventType, cancellationToken);
            var settings = settingsArray == null ? null : JsonSerializer.Deserialize<AuditSettings?>(settingsArray);
            settingsList.Add(eventType, settings);
        }

        return settingsList;
    }

    public async Task AddRangeAsync(IEnumerable<AuditSettings> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            await distributedCache
                .SetAsync(setting.EventType,
                    JsonSerializer.SerializeToUtf8Bytes(setting),
                    cancellationToken);
        }
    }
}