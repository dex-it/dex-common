using Dex.DistributedCache.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.DistributedCache.Extensions
{
    public static class DistributedCacheExtension
    {
        public static IServiceCollection AddDistributedCache(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<ICacheDependencyFactory, CacheDependencyFactory>()
                .AddTransient<ICacheActionFilterService, CacheActionFilterService>()
                .AddTransient<ICacheVariableKeyFactory, CacheUserVariableFactory>();
        }
    }
}