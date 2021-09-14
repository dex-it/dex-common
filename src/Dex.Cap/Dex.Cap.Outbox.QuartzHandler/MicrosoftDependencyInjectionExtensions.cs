using System;
using Dex.Cap.Outbox.Scheduler.BackgroundServices;
using Dex.Cap.Outbox.Scheduler.Interfaces;
using Dex.Cap.Outbox.Scheduler.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Scheduler
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection RegisterOutboxScheduler(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (periodSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(periodSeconds), periodSeconds, "Should be a positive number");
            }
            
            if (cleanupDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cleanupDays), cleanupDays, "Should be a positive number");
            }

            services
                .AddSingleton(new OutboxHandlerOptions() 
                {
                    Period = TimeSpan.FromSeconds(periodSeconds),
                    CleanupOlderThan = TimeSpan.FromDays(cleanupDays)
                })
                .AddScoped<IOutboxCleanerHandler, OutboxCleanerHandler>()
                .AddHostedService<OutboxHandlerBackgroundService>()
                .AddHostedService<OutboxCleanerBackgroundService>();

            return services;
        }
    }
}