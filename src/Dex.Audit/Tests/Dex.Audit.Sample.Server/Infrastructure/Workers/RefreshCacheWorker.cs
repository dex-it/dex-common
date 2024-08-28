using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
using Microsoft.Extensions.Options;

namespace Dex.Audit.ServerSample.Infrastructure.Workers;

/// <summary>
/// Фоновая служба для обновления кэша.
/// </summary>
/// <param name="logger">Логгер для записи информации о работе фоновой службы.</param>
/// <param name="scopeFactory">Провайдер для создания области служб.</param>
/// <param name="options">Настройки кэширования.</param>
public sealed class RefreshCacheWorker(
    ILogger<RefreshCacheWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<AuditCacheOptions> options) : BackgroundService
{
    /// <summary>
    /// Метод, который будет выполняться асинхронно в фоновом режиме.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для прерывания операции.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogDebug("The audit cache update service has started working.");

                await UpdateCache(stoppingToken).ConfigureAwait(false);

                logger.LogDebug("Cache updated successfully.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while updating the audit cache.");
            }

            await Task.Delay(options.Value.RefreshInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Обновить записи в кэше.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task UpdateCache(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditSettingsRepository>();
        var auditSettingsRepository = scope.ServiceProvider.GetRequiredService<IAuditSettingsCacheRepository>();

        var auditSettings = await auditRepository.GetAllSettingsAsync(cancellationToken).ConfigureAwait(false);

        await auditSettingsRepository.AddRangeAsync(auditSettings, cancellationToken).ConfigureAwait(false);
    }
}
