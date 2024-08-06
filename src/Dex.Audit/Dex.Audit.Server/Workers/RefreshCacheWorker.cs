using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Server.Workers;

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
                logger.LogInformation("Служба обновления кеша аудита начала работу");
                await UpdateCache(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Возникла ошибка при обновлении кеша аудита");
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
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditPersistentRepository>();
        var auditSettingsRepository = scope.ServiceProvider.GetRequiredService<IAuditCacheRepository>();

        logger.LogInformation("Выполняется операция обновления настроек аудита в кэше");

        var refreshInterval = options.Value.RefreshInterval;

        var auditSettings = await auditRepository.GetAllSettingsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var setting in auditSettings)
        {
            await auditSettingsRepository.AddAsync(setting, refreshInterval, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Кэш успешно обновлен");
    }
}
