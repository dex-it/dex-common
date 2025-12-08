using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.AspNetScheduler.BackgroundServices;

internal sealed class OutboxCleanerBackgroundService(
    IServiceScopeFactory scopeFactory,
    OutboxHandlerOptions options,
    ILogger<OutboxCleanerBackgroundService> logger)
    : BackgroundService
{
    private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
    private const string TypeName = nameof(OutboxCleanerBackgroundService);

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
                    var loggerInner = (ILogger)scope.ServiceProvider.GetRequiredService(typeof(ILogger<OutboxCleanerBackgroundService>));
                    loggerInner.LogDebug("Background service '{ServiceName}' Tick event", GetType());

                    await OnTick(scope.ServiceProvider, loggerInner, stoppingToken).ConfigureAwait(false);
                }

                logger.LogDebug("Pause for {Seconds} seconds", (int)options.CleanupInterval.TotalSeconds);
                await Task.Delay(options.CleanupInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task OnTick(IServiceProvider serviceProvider, ILogger loggerInner, CancellationToken cancellationToken)
    {
        loggerInner.LogDebug("Resolving Outbox cleaner");
        var service = serviceProvider.GetRequiredService<IOutboxCleanerHandler>();

        loggerInner.LogDebug("Executing Outbox cleaner");
        try
        {
            await service.Execute(options.CleanupOlderThan, cancellationToken).ConfigureAwait(false);
            loggerInner.LogDebug("Outbox cleaner finished");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            loggerInner.LogDebug("Outbox cleaner was interrupted by stopping of host process");
            throw;
        }
        catch (Exception ex)
        {
            loggerInner.LogCritical(ex, "Critical error in Outbox cleaner '{ServiceName}'", service.GetType());
        }
    }

    /// <summary>
    /// Решаем проблему "расщеплённого мозга".
    /// </summary>
    private Task InitDelay(CancellationToken cancellationToken)
    {
        var initDelay = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(20_000, 40_000));
        logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
        return Task.Delay(initDelay, cancellationToken);
    }
}