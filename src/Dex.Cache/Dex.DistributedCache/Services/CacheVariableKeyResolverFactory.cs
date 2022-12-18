using System;

namespace Dex.DistributedCache.Services
{
    internal sealed class CacheVariableKeyResolverFactory : ICacheVariableKeyResolverFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CacheVariableKeyResolverFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICacheVariableKeyResolver? GetCacheVariableKeyResolverService(Type type)
        {
            if (_serviceProvider.GetService(type) is ICacheVariableKeyResolver cacheVariableKeyResolver)
            {
                return cacheVariableKeyResolver;
            }

            return null;
        }
    }
}