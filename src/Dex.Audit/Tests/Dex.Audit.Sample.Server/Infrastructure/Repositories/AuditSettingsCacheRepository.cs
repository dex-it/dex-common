using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.ServerSample.Infrastructure.Repositories;

internal class AuditSettingsCacheRepository(IRedisDatabase redisDatabase) : IAuditSettingsCacheRepository
{
    public Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return redisDatabase.GetAsync<AuditSettings>(eventType);
    }

    public Task<IDictionary<string, AuditSettings?>> GetDictionaryAsync(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken = default)
    {
        return redisDatabase.GetAllAsync<AuditSettings>(eventTypes.ToHashSet());
    }

    public async Task AddRangeAsync(IEnumerable<AuditSettings> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            await redisDatabase.AddAsync(setting.EventType, setting);
        }
    }
}