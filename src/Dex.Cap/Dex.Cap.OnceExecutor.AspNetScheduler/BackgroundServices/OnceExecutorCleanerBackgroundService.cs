using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.AspNetScheduler.Interfaces;
using Dex.Cap.OnceExecutor.AspNetScheduler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.OnceExecutor.AspNetScheduler.BackgroundServices;

internal sealed class OnceExecutorCleanerBackgroundService(
    IServiceScopeFactory scopeFactory,
    OnceExecutorHandlerOptions options,
    ILogger<OnceExecutorCleanerBackgroundService> logger)
    : BackgroundService
{
    private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
    private const string TypeName = nameof(OnceExecutorCleanerBackgroundService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(ServiceNameIsStatus, TypeName, "starting");

        // Решаем проблему "расщеплённого мозга".
        await InitDelay(stoppingToken).ConfigureAwait(false);

        await using (stoppingToken.Register(static s => ((ILogger)s!).LogInformation(ServiceNameIsStatus, TypeName, "stopping"), logger))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var scopedLogger = (ILogger)scope.ServiceProvider.GetRequiredService(typeof(ILogger<OnceExecutorCleanerBackgroundService>));
                    scopedLogger.LogDebug("Background service '{ServiceName}' Tick event", GetType());

                    await OnTick(scope.ServiceProvider, scopedLogger, stoppingToken).ConfigureAwait(false);
                }

                logger.LogDebug("Pause for {Seconds} seconds", (int)options.CleanupInterval.TotalSeconds);
                await Task.Delay(options.CleanupInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    /// <exception cref="OperationCanceledException"/>
    private async Task OnTick(IServiceProvider serviceProvider, ILogger scopedLogger, CancellationToken cancellationToken)
    {
        scopedLogger.LogDebug("Resolving OnceExecutor cleaner");
        var service = serviceProvider.GetRequiredService<IOnceExecutorCleanerHandler>();

        scopedLogger.LogDebug("Executing OnceExecutor cleaner");
        try
        {
            await service.Execute(options.CleanupOlderThan, cancellationToken).ConfigureAwait(false);
            scopedLogger.LogDebug("OnceExecutor cleaner finished");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            scopedLogger.LogDebug("OnceExecutor cleaner was interrupted by stopping of host process");
            throw;
        }
        catch (Exception ex)
        {
            scopedLogger.LogCritical(ex, "Critical error in OnceExecutor cleaner '{ServiceName}'", service.GetType());
        }
    }

    private Task InitDelay(CancellationToken cancellationToken)
    {
        var initDelay = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(20_000, 40_000));
        logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
        return Task.Delay(initDelay, cancellationToken);
    }
}