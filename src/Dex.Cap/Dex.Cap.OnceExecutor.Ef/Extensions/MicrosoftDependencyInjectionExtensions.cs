using System;
using Dex.Cap.Common.Ef;
using Dex.Cap.OnceExecutor.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.Ef.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    /// <summary>
    /// Adding OnceExecutorEf
    /// Also, RegisterOnceExecutorScheduler is recommended for better performance
    /// </summary>
    public static IServiceCollection AddOnceExecutor<TDbContext>(this IServiceCollection serviceProvider)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider
            .AddScoped(typeof(IOnceExecutor<IEfTransactionOptions, TDbContext>), typeof(OnceExecutorEf<TDbContext>))
            .AddScoped(typeof(IOnceExecutor<IEfTransactionOptions>), typeof(OnceExecutorEf<TDbContext>))
            .AddScoped<IOnceExecutorCleanupDataProvider, OnceExecutorCleanupDataProviderEf<TDbContext>>();
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
}