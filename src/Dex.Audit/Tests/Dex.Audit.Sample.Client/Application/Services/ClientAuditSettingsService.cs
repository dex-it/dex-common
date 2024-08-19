using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.ClientSample.Application.Services;

public class ClientAuditSettingsService(IAuditCacheRepository cacheRepository, IConfiguration configuration) : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var setting = await cacheRepository.GetAsync(eventType, cancellationToken);

        if (setting != null)
        {
            return setting;
        }

        using var httpClient = new HttpClient();
        using var result = await httpClient.GetAsync(configuration.GetConnectionString("ServerAddress") + "/Settings", cancellationToken);
        var settings = await result.Content.ReadFromJsonAsync<IEnumerable<AuditSettings>>(cancellationToken: cancellationToken);
        if (settings == null)
        {
            return null;
        }

        await cacheRepository
            .AddRangeAsync(settings, cancellationToken)
            .ConfigureAwait(false);

        return await cacheRepository.GetAsync(eventType, cancellationToken);
    }
}