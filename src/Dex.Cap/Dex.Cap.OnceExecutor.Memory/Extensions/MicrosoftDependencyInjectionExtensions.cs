using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dex.Cap.OnceExecutor.Memory.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddOnceExecutor<TDistributedCache>(this IServiceCollection services,
        Action<MemoryDistributedCacheOptions> globalCacheOptions, Action<DistributedCacheEntryOptions> entryOptions)
        where TDistributedCache : class, IDistributedCache
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();
        services.Configure(entryOptions);
        services.Configure(globalCacheOptions);
        services.TryAdd(ServiceDescriptor.Singleton<TDistributedCache, TDistributedCache>());

        return services
            .AddScoped(typeof(IOnceExecutor<IOnceExecutorMemoryOptions, IDistributedCache>), typeof(OnceExecutorMemory<TDistributedCache>))
            .AddScoped(typeof(IOnceExecutor<IOnceExecutorMemoryOptions>), typeof(OnceExecutorMemory<TDistributedCache>));
    }
}