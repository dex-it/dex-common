using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Server.Workers;

/// <summary>
/// Background service for cache updating.
/// </summary>
/// <param name="logger">Logger for recording information about the background service's work.</param>
/// <param name="scopeFactory">Provider for creating service scopes.</param>
/// <param name="options">Caching settings.</param>
public class RefreshCacheWorker(
    ILogger<RefreshCacheWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<AuditCacheOptions> options) : BackgroundService
{
    /// <summary>
    /// Method that will be executed asynchronously in the background.
    /// </summary>
    /// <param name="stoppingToken"><see cref="CancellationToken"/></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogDebug("The audit cache update service has started working.");

                await UpdateCache(stoppingToken)
                    .ConfigureAwait(false);

                logger.LogDebug("Cache updated successfully.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while updating the audit cache.");
            }

            await Task.Delay(options.Value.RefreshInterval, stoppingToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Update cache records.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task UpdateCache(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditSettingsRepository>();
        var auditSettingsRepository = scope.ServiceProvider.GetRequiredService<IAuditSettingsCacheRepository>();

        var auditSettings = await auditRepository
            .GetAllSettingsAsync(cancellationToken)
            .ConfigureAwait(false);

        await auditSettingsRepository
            .AddRangeAsync(auditSettings, cancellationToken)
            .ConfigureAwait(false);
    }
}
