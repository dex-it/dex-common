using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.ClientSample.Services;

public class ClientAuditSettingsService(IAuditCacheRepository repository) : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await repository.GetAsync(eventType, cancellationToken);
    }
}