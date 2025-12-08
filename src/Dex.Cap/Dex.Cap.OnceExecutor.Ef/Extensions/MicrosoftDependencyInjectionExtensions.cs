using System;
using Dex.Cap.Common.Ef;
using Dex.Cap.OnceExecutor.AspNetScheduler;
using Dex.Cap.OnceExecutor.AspNetScheduler.BackgroundServices;
using Dex.Cap.OnceExecutor.AspNetScheduler.Interfaces;
using Dex.Cap.OnceExecutor.AspNetScheduler.Options;
using Dex.Cap.OnceExecutor.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.Ef.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddOnceExecutor<TDbContext>(this IServiceCollection serviceProvider)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider
            .AddScoped(typeof(IOnceExecutor<IEfTransactionOptions, TDbContext>), typeof(OnceExecutorEf<TDbContext>))
            .AddScoped(typeof(IOnceExecutor<IEfTransactionOptions>), typeof(OnceExecutorEf<TDbContext>));
    }

    public static IServiceCollection AddStrategyOnceExecutor<TArg, TResult, TExecutionStrategy, TDbContext>(this IServiceCollection serviceProvider)
        where TDbContext : DbContext
        where TExecutionStrategy : IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider
            .AddScoped(typeof(IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>), typeof(TExecutionStrategy))
            .AddScoped(typeof(IStrategyOnceExecutor<TArg, TResult>),
                typeof(StrategyOnceExecutorEf<TArg, TResult, IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>, TDbContext>));
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection AddDefaultOnceExecutorScheduler<TDbContext>(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TDbContext : DbContext
    {
        return AddOnceExecutorScheduler<OnceExecutorCleanupDataProviderEf<TDbContext>>(services, periodSeconds, cleanupDays);
    }

    /// <summary>
    /// To clean obsolete db-records, to improve performance
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection AddOnceExecutorScheduler<TCleanUpDataProvider>(this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TCleanUpDataProvider : class, IOnceExecutorCleanupDataProvider
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
            .AddScoped<IOnceExecutorCleanupDataProvider, TCleanUpDataProvider>()
            .AddScoped<IOnceExecutorCleanerHandler, OnceExecutorCleanerHandler>()
            .AddHostedService<OnceExecutorCleanerBackgroundService>();

        return services;
    }
}