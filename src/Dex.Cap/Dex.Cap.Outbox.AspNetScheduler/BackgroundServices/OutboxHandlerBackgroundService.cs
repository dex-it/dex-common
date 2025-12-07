using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.AspNetScheduler.BackgroundServices;

internal sealed class OutboxHandlerBackgroundService(
    IServiceScopeFactory scopeFactory,
    OutboxHandlerOptions options,
    ILogger<OutboxHandlerBackgroundService> logger)
    : BackgroundService
{
    private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
    private const string TypeName = nameof(OutboxHandlerBackgroundService);

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
                    var loggerInner = (ILogger)scope.ServiceProvider.GetRequiredService(typeof(ILogger<OutboxHandlerBackgroundService>));
                    logger.LogDebug("Background service '{ServiceName}' Tick event", GetType());

                    await OnTick(scope.ServiceProvider, loggerInner, stoppingToken).ConfigureAwait(false);
                }

                logger.LogDebug("Pause for {Seconds} seconds", (int)options.Period.TotalSeconds);
                await Task.Delay(options.Period, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task OnTick(IServiceProvider serviceProvider, ILogger loggerInner, CancellationToken cancellationToken)
    {
        loggerInner.LogDebug("Resolving IOutboxHandler");
        var service = serviceProvider.GetRequiredService<IOutboxHandler>();

        loggerInner.LogDebug("Executing Outbox handler");
        try
        {
            await service.ProcessAsync(cancellationToken).ConfigureAwait(false);
            loggerInner.LogDebug("Outbox handler finished");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            loggerInner.LogDebug("Outbox handler was interrupted by stopping of host process");
            throw;
        }
        catch (Exception ex)
        {
            loggerInner.LogCritical(ex, "Critical error in Outbox background handler '{ServiceName}'", service.GetType());
        }
    }

    /// <summary>
    /// Решаем проблему "расщеплённого мозга".
    /// </summary>
    private Task InitDelay(CancellationToken cancellationToken)
    {
        var initDelay = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(5000, 15_000));
        logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
        return Task.Delay(initDelay, cancellationToken);
    }
}