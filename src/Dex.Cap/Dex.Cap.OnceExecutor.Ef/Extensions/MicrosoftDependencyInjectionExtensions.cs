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

        public static IServiceCollection AddStrategyOnceExecutor<TArg, TResult, TExecutionStrategyInterface, TExecutionStrategyImplementation, TDbContext>(
            this IServiceCollection serviceProvider)
            where TDbContext : DbContext
            where TExecutionStrategyInterface : IOnceExecutionStrategy<TArg, TResult>
            where TExecutionStrategyImplementation : class, IOnceExecutionStrategy<TArg, TResult>
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            return serviceProvider
                .AddScoped(typeof(TExecutionStrategyInterface), typeof(TExecutionStrategyImplementation))
                .AddScoped(typeof(IStrategyOnceExecutor<TArg, TResult, TExecutionStrategyInterface>),
                    typeof(StrategyOnceExecutorEf<TArg, TResult, TExecutionStrategyInterface, TDbContext>));
        }
    }
}