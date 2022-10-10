using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.DistributedCache.Services
{
    class CacheUserVariableFactory : ICacheUserVariableFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CacheUserVariableFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICacheUserVariableService GetCacheUserVariableService()
        {
            return _serviceProvider.GetRequiredService<ICacheUserVariableService>();
        }
    }
}