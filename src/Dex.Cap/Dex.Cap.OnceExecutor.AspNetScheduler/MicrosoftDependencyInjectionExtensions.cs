using System;
using Dex.Cap.OnceExecutor.AspNetScheduler.BackgroundServices;
using Dex.Cap.OnceExecutor.AspNetScheduler.Interfaces;
using Dex.Cap.OnceExecutor.AspNetScheduler.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.AspNetScheduler;

public static class MicrosoftDependencyInjectionExtensions
{
    /// <summary>
    /// To clean obsolete db-records, to improve performance
    /// AddOnceExecutor in DI is required
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection RegisterOnceExecutorScheduler(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 180)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (periodSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds), periodSeconds, "Should be a positive number");

        if (cleanupDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(cleanupDays), cleanupDays, "Should be a positive number");

        services
            .AddSingleton(new OnceExecutorHandlerOptions
            {
                CleanupOlderThan = TimeSpan.FromDays(cleanupDays),
                CleanupInterval = TimeSpan.FromHours(1)
            })
            .AddScoped<IOnceExecutorCleanerHandler, OnceExecutorCleanerHandler>()
            .AddHostedService<OnceExecutorCleanerBackgroundService>();

        return services;
    }
}