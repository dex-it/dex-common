using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Dex.Audit.Implementations.Common.Repositories;

public class SimpleAuditSettingsCacheRepository(IMemoryCache memoryCache)
    : IAuditSettingsCacheRepository
{
    public Task<AuditSettings?> GetAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var settingsArray = memoryCache.Get<AuditSettings?>(eventType);
        return Task.FromResult(settingsArray);
    }

    public Task<IDictionary<string, AuditSettings?>> GetDictionaryAsync(IEnumerable<string> eventTypes,
        CancellationToken cancellationToken = default)
    {
        var enumerable = eventTypes as string[] ?? eventTypes.ToArray();

        IDictionary<string, AuditSettings?> settingsDictionary = 
            new Dictionary<string, AuditSettings?>(enumerable.Length);

        foreach (var eventType in enumerable)
        {
            var auditSettings = memoryCache.Get<AuditSettings?>(eventType);
            settingsDictionary.Add(eventType, auditSettings);
        }

        return Task.FromResult(settingsDictionary);
    }

    public Task AddRangeAsync(IEnumerable<AuditSettings> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            memoryCache
                .Set(setting.EventType,
                    setting);
        }

        return Task.CompletedTask;
    }
}