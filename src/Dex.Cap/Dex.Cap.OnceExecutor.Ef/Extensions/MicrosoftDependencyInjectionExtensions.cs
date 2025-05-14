using System;
using Dex.Cap.Common.Ef;
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
    }
}