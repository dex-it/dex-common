using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.ServerSample.Repositories;

public class AuditSettingsRepository(IRedisDatabase redisDatabase) : IAuditSettingsRepository
{
    public async Task<AuditSettings?> GetAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await redisDatabase.GetAsync<AuditSettings>(eventType);
    }

    public async Task AddAsync(string settingEventType, AuditSettings settings, TimeSpan refreshInterval,
        CancellationToken cancellationToken = default)
    {
        await redisDatabase.AddAsync(settingEventType, settings, refreshInterval);
    }
}