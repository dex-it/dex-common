using System;

namespace Dex.DistributedCache.Services
{
    internal sealed class CacheUserVariableFactory : ICacheVariableKeyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CacheUserVariableFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICacheVariableKey? GetCacheVariableKeyService(Type type)
        {
            if (_serviceProvider.GetService(type) is ICacheVariableKey cacheVariableKeyService)
            {
                return cacheVariableKeyService;
            }

            return null;
        }
    }
}