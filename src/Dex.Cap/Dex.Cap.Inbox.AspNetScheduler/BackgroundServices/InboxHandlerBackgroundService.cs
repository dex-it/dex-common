using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.AspNetScheduler.BackgroundServices;

internal sealed class InboxHandlerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxHandlerOptions> options,
    IOptions<InboxOptions> inboxOptions,
    ILogger<InboxHandlerBackgroundService> logger)
    : BackgroundService
{
    private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
    private const string TypeName = nameof(InboxHandlerBackgroundService);

    /// <remarks>
    /// Снимок опций на старте: hosted-сервис живёт всё время работы хоста, hot-reload не поддерживается
    /// намеренно.
    /// </remarks>
    private readonly InboxHandlerOptions _options = options.Value;

    private readonly int _messagesToProcess = inboxOptions.Value.MessagesToProcess;

    /// <summary>
    /// Разбирать инбокс, пока хост жив.
    /// </summary>
    /// <remarks>
    /// Пауза делается, только когда партия пришла неполной, то есть очередь исчерпана. Безусловная пауза
    /// после каждого цикла превратила бы Period в жёсткий потолок пропускной способности
    /// (MessagesToProcess сообщений за Period) независимо от реальной нагрузки.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(ServiceNameIsStatus, TypeName, "starting");

        await InitDelay(stoppingToken).ConfigureAwait(false);

        await using (stoppingToken.Register(static s => ((ILogger)s!).LogInformation(ServiceNameIsStatus, TypeName, "stopping"), logger))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int processed;

                using (var scope = scopeFactory.CreateScope())
                {
                    var loggerInner = scope.ServiceProvider.GetRequiredService<ILogger<InboxHandlerBackgroundService>>();
                    loggerInner.LogDebug("Background service '{ServiceName}' Tick event", TypeName);

                    processed = await OnTick(scope.ServiceProvider, loggerInner, stoppingToken).ConfigureAwait(false);
                }

                if (processed >= _messagesToProcess)
                {
                    continue;
                }

                logger.LogDebug("Pause for {Seconds} seconds", (int)_options.Period.TotalSeconds);
                await Task.Delay(_options.Period, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Один цикл разбора инбокса.
    /// </summary>
    /// <remarks>
    /// Цикл переживает ошибку тика, но нулевой результат уводит следующий заход в паузу: при устойчивом
    /// сбое (например, недоступна БД) разбор без паузы превратился бы в busy-loop.
    /// </remarks>
    /// <returns>Количество обработанных сообщений; ноль в том числе при ошибке тика.</returns>
    private static async Task<int> OnTick(IServiceProvider serviceProvider, ILogger loggerInner, CancellationToken cancellationToken)
    {
        loggerInner.LogDebug("Resolving IInboxHandler");
        var service = serviceProvider.GetRequiredService<IInboxHandler>();

        loggerInner.LogDebug("Executing Inbox handler");
        try
        {
            var processed = await service.ProcessAsync(cancellationToken).ConfigureAwait(false);
            loggerInner.LogDebug("Inbox handler finished, processed {Count} messages", processed);
            return processed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            loggerInner.LogDebug("Inbox handler was interrupted by stopping of host process");
            throw;
        }
        catch (Exception ex)
        {
            loggerInner.LogCritical(ex, "Critical error in Inbox background handler '{ServiceName}'", service.GetType());

            return 0;
        }
    }

    /// <summary>
    /// Решаем проблему "расщеплённого мозга".
    /// </summary>
    private Task InitDelay(CancellationToken cancellationToken)
    {
        var initDelay = _options.HandlerInitDelay.GetDelay();
        logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
        return Task.Delay(initDelay, cancellationToken);
    }
}
