using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Interfaces;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.AspNetScheduler.BackgroundServices;

internal sealed class InboxCleanerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxHandlerOptions> options,
    ILogger<InboxCleanerBackgroundService> logger)
    : BackgroundService
{
    private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
    private const string TypeName = nameof(InboxCleanerBackgroundService);

    /// <remarks>
    /// Снимок опций на старте: hosted-сервис живёт всё время работы хоста, hot-reload не поддерживается
    /// намеренно.
    /// </remarks>
    private readonly InboxHandlerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(ServiceNameIsStatus, TypeName, "starting");

        await InitDelay(stoppingToken).ConfigureAwait(false);

        await using (stoppingToken.Register(static s => ((ILogger)s!).LogInformation(ServiceNameIsStatus, TypeName, "stopping"), logger))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var loggerInner = scope.ServiceProvider.GetRequiredService<ILogger<InboxCleanerBackgroundService>>();
                    loggerInner.LogDebug("Background service '{ServiceName}' Tick event", TypeName);

                    await OnTick(scope.ServiceProvider, loggerInner, stoppingToken).ConfigureAwait(false);
                }

                logger.LogDebug("Pause for {Seconds} seconds", (int)_options.CleanupInterval.TotalSeconds);
                await Task.Delay(_options.CleanupInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task OnTick(IServiceProvider serviceProvider, ILogger loggerInner, CancellationToken cancellationToken)
    {
        loggerInner.LogDebug("Resolving Inbox cleaner");
        var service = serviceProvider.GetRequiredService<IInboxCleanerHandler>();

        loggerInner.LogDebug("Executing Inbox cleaner");
        try
        {
            await service.Execute(_options.CleanupOlderThan, cancellationToken).ConfigureAwait(false);
            loggerInner.LogDebug("Inbox cleaner finished");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            loggerInner.LogDebug("Inbox cleaner was interrupted by stopping of host process");
            throw;
        }
        catch (Exception ex)
        {
            loggerInner.LogCritical(ex, "Critical error in Inbox cleaner '{ServiceName}'", service.GetType());
        }
    }

    /// <summary>
    /// Решаем проблему "расщеплённого мозга".
    /// </summary>
    private Task InitDelay(CancellationToken cancellationToken)
    {
        var initDelay = _options.CleanerInitDelay.GetDelay();
        logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
        return Task.Delay(initDelay, cancellationToken);
    }
}