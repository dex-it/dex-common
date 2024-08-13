using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.Sample.Shared.Repositories;

public class AuditCacheRepository(IRedisDatabase redisDatabase) : IAuditCacheRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await redisDatabase.GetAsync<AuditSettings>(eventType);
    }

    public async Task AddRangeAsync(IEnumerable<AuditSettings> settings, CancellationToken cancellationToken = default)
    {
        foreach (var setting in settings)
        {
            await redisDatabase.AddAsync(setting.EventType, setting);
        }
    }
}