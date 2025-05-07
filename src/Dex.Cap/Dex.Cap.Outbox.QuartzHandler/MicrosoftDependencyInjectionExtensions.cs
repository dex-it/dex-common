using System;
using Dex.Cap.Outbox.AspNetScheduler.BackgroundServices;
using Dex.Cap.Outbox.AspNetScheduler.Interfaces;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.AspNetScheduler
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection RegisterOutboxScheduler(this IServiceCollection services,
            int periodSeconds = 30, int cleanupDays = 30)
        {
            ArgumentNullException.ThrowIfNull(services);

            if (periodSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(periodSeconds), periodSeconds,
                    "Should be a positive number");
            }

            if (cleanupDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cleanupDays), cleanupDays, "Should be a positive number");
            }

            services.AddHealthChecks()
                .AddCheck<OutboxHealthCheck>("outbox-scheduler", tags: ["outbox-scheduler"]);

            services
                .AddSingleton(new OutboxHandlerOptions
                {
                    Period = TimeSpan.FromSeconds(periodSeconds),
                    CleanupOlderThan = TimeSpan.FromDays(cleanupDays),
                    CleanupInterval = TimeSpan.FromHours(1)
                })
                .AddScoped<IOutboxCleanerHandler, OutboxCleanerHandler>()
                .AddHostedService<OutboxHandlerBackgroundService>()
                .AddHostedService<OutboxCleanerBackgroundService>();

            return services;
        }
    }
}