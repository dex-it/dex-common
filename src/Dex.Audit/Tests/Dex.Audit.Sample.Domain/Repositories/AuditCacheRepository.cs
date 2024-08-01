using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.Sample.Domain.Repositories;

public class AuditCacheRepository(IRedisDatabase redisDatabase) : IAuditCacheRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await redisDatabase.GetAsync<AuditSettings>(eventType);
    }

    public async Task AddAsync(AuditSettings settings, TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        await redisDatabase.AddAsync(settings.EventType, settings, expiresIn);
    }
}