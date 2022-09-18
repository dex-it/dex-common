using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.Scheduler.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.AspNetScheduler.BackgroundServices
{
    internal sealed class OutboxCleanerBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly OutboxHandlerOptions _options;
        private const string ServiceNameIsStatus = "Background service '{ServiceName}' is {Status}";
        private const string TypeName = nameof(OutboxCleanerBackgroundService);

        // Инжектим только синглтоны.
        public OutboxCleanerBackgroundService(IServiceScopeFactory scopeFactory, OutboxHandlerOptions options,
            ILogger<OutboxCleanerBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(ServiceNameIsStatus, TypeName, "starting");

            // Решаем проблему "расщеплённого мозга".
            await InitDelay(stoppingToken);

            await using (stoppingToken.Register(static s => ((ILogger)s!).LogInformation(ServiceNameIsStatus, TypeName, "stopping"), _logger))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var logger = (ILogger)scope.ServiceProvider.GetRequiredService(typeof(ILogger<OutboxCleanerBackgroundService>));
                        logger.LogTrace("Background service '{ServiceName}' Tick event", GetType());

                        await OnTick(scope.ServiceProvider, logger, stoppingToken);
                    }

                    _logger.LogTrace("Pause for {Seconds} seconds", (int)_options.CleanupInterval.TotalSeconds);
                    await Task.Delay(_options.CleanupInterval, stoppingToken);
                }
            }
        }

        /// <exception cref="OperationCanceledException"/>
        private async Task OnTick(IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogTrace("Resolving Outbox cleaner");
            var service = serviceProvider.GetRequiredService<IOutboxCleanerHandler>();

            logger.LogTrace("Executing Outbox cleaner");
            try
            {
                await service.Execute(_options.CleanupOlderThan, cancellationToken);
                logger.LogTrace("Outbox cleaner finished");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogTrace("Outbox cleaner was interrupted by stopping of host process");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Critical error in Outbox cleaner '{ServiceName}'", service.GetType());
            }
        }

        private async Task InitDelay(CancellationToken cancellationToken)
        {
            var initDelay = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(20_000, 40_000));
            _logger.LogDebug("Initial delay for {Seconds} seconds to solve split brain problem", (int)initDelay.TotalSeconds);
            await Task.Delay(initDelay, cancellationToken);
        }
    }
}