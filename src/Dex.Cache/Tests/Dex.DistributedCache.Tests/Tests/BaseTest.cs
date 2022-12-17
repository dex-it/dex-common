using Dex.DistributedCache.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.DistributedCache.Tests.Tests
{
    public abstract class BaseTest
    {
        protected IServiceCollection InitServiceCollection()
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddDebug();
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .AddDistributedCache()
                .AddSingleton<IDistributedCache, MemoryDistributedCache>();

            return serviceCollection;
        }
    }
}