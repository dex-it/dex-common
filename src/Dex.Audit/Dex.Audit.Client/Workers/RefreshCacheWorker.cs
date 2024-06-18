using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Options;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Workers;

/// <summary>
/// Фоновая служба для обновления кэша
/// </summary>
internal sealed class RefreshCacheWorker : BackgroundService
{
    private readonly ILogger<RefreshCacheWorker> _logger;
    private readonly AuditCacheOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="RefreshCacheWorker"/>
    /// </summary>
    /// <param name="logger">Логгер для записи информации о работе фоновой службы</param>
    /// <param name="scopeFactory">Провайдер для создания области служб</param>
    /// <param name="options">Настройки кэширования</param>
    public RefreshCacheWorker(ILogger<RefreshCacheWorker> logger, IServiceScopeFactory scopeFactory, IOptions<AuditCacheOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    /// <summary>
    /// Метод, который будет выполняться асинхронно в фоновом режиме
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для прерывания операции</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Служба обновления кеша аудита начала работу");
                await UpdateCache(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникла ошибка при обновлении кеша аудита");
            }

            await Task.Delay(_options.RefreshInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Обновить записи в кэше
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task UpdateCache(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAuditRepository context = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        IAuditSettingsRepository redisDatabase = scope.ServiceProvider.GetRequiredService<IAuditSettingsRepository>();

        _logger.LogInformation("Выполняется операция обновления настроек аудита в кэше");

        TimeSpan refreshInterval = _options.RefreshInterval;

        List<AuditSettings> auditSettings = await context.GetAllSettingsAsync(cancellationToken);

        foreach (AuditSettings setting in auditSettings)
        {
            await redisDatabase.AddAsync(setting.EventType, setting, refreshInterval);
        }

        _logger.LogInformation("Кэш успешно обновлен");
    }
}
