using Dex.DistributedCache.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.DistributedCache.Extensions
{
    public static class MicrosoftDependencyInjectionExtension
    {
        public static IServiceCollection AddDistributedCache(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<ICacheManagementService, CacheService>(provider => (CacheService)provider.GetRequiredService<ICacheService>())
                .AddSingleton<ICacheDependencyFactory, CacheDependencyFactory>()
                .AddTransient<ICacheActionFilterService, CacheActionFilterService>()
                .AddTransient<ICacheVariableKeyFactory, CacheUserVariableFactory>();
        }

        public static IServiceCollection RegisterCacheDependencyService<TValue, TService>(this IServiceCollection services)
            where TService : class, ICacheDependencyService<TValue>
        {
            return services.AddSingleton<ICacheDependencyService<TValue>, TService>();
        }

        public static IServiceCollection RegisterCacheVariableKeyService<TInterface, TService>(this IServiceCollection services)
            where TInterface : ICacheVariableKey
            where TService : class, ICacheVariableKey
        {
            return services.AddTransient(typeof(TInterface), typeof(TService));
        }
    }
}