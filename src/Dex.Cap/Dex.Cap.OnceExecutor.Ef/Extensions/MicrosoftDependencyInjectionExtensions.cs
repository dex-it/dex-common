using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.Ef.Extensions
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOnceExecutor<TDbContext>(this IServiceCollection serviceProvider)
            where TDbContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            return serviceProvider
                .AddScoped(typeof(IOnceExecutor<IEfOptions, TDbContext>), typeof(OnceExecutorEf<TDbContext>))
                .AddScoped(typeof(IOnceExecutor<IEfOptions>), typeof(OnceExecutorEf<TDbContext>));
        }

        public static IServiceCollection AddStrategyOnceExecutor<TArg, TResult, TExecutionStrategy, TDbContext>(this IServiceCollection serviceProvider)
            where TDbContext : DbContext
            where TExecutionStrategy : IOnceExecutionStrategy<TArg, IEfOptions, TResult>
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            return serviceProvider
                .AddScoped(typeof(IOnceExecutionStrategy<TArg, IEfOptions, TResult>), typeof(TExecutionStrategy))
                .AddScoped(typeof(IStrategyOnceExecutor<TArg, TResult>),
                    typeof(StrategyOnceExecutorEf<TArg, TResult, IOnceExecutionStrategy<TArg, IEfOptions, TResult>, TDbContext>));
        }
    }
}