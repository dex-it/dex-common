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
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            return serviceProvider
                .AddScoped(typeof(IOnceExecutor<TDbContext>), typeof(OnceExecutorEf<TDbContext>))
                .AddScoped(typeof(IOnceExecutor), typeof(OnceExecutorEf<TDbContext>));
        }

        public static IServiceCollection AddStrategyOnceExecutor<TArg, TResult, TExecutionStrategy, TDbContext>(this IServiceCollection serviceProvider)
            where TDbContext : DbContext
            where TExecutionStrategy : IOnceExecutionStrategy<TArg, TResult>
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            return serviceProvider
                .AddScoped(typeof(IOnceExecutionStrategy<TArg, TResult>), typeof(TExecutionStrategy))
                .AddScoped(typeof(IStrategyOnceExecutor<TArg, TResult>),
                    typeof(StrategyOnceExecutorEf<TArg, TResult, IOnceExecutionStrategy<TArg, TResult>, TDbContext>));
        }
    }
}