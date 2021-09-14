using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Scheduler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.Scheduler.BackgroundServices
{
    internal sealed class OutboxHandlerBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly OutboxHandlerOptions _options;

        // Инжектим только синглтоны.
        public OutboxHandlerBackgroundService(IServiceScopeFactory scopeFactory, OutboxHandlerOptions options,
                                              ILogger<OutboxHandlerBackgroundService> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Background service '{ServiceName}' is starting", GetType().Name);

            // Решаем проблему "расщеплённого мозга".
            await InitDelay(stoppingToken);

            using (stoppingToken.Register(static s => ((ILogger)s!).LogDebug("Background service is stopping"), _logger))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var logger = (ILogger)scope.ServiceProvider.GetRequiredService(typeof(ILogger<OutboxHandlerBackgroundService>));
                        logger.LogTrace("Background service '{ServiceName}' Tick event", GetType());
                        
                        await OnTick(scope.ServiceProvider, logger, stoppingToken);
                    }

                    _logger.LogTrace("Pause for {Seconds} seconds", (int)_options.Period.TotalSeconds);
                    await Task.Delay(_options.Period, stoppingToken);
                }
            }
        }

        private static async Task OnTick(IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogTrace("Resolving IOutboxHandler");
            var service = serviceProvider.GetRequiredService<IOutboxHandler>();

            logger.LogTrace("Executing Outbox handler");
            try
            {
                await service.ProcessAsync(cancellationToken);
                logger.LogTrace("Outbox handler finished");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Outbox handler was interrupted by stopping of host process");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Critical error in Outbox background handler '{ServiceName}'", service.GetType());
            }
        }

        private async Task InitDelay(CancellationToken cancellationToken)
        {
            var initDelay = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(5000, 15_000));
            _logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
            await Task.Delay(initDelay, cancellationToken);
        }
    }
}
