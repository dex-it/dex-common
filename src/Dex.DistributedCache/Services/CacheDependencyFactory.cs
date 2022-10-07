using System;

namespace Dex.DistributedCache.Services
{
    public sealed class CacheDependencyFactory : ICacheDependencyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CacheDependencyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICacheDependencyService? GetCacheDependencyService(Type type)
        {
            var genericType = typeof(ICacheDependencyService<>).MakeGenericType(type);
            if (_serviceProvider.GetService(genericType) is ICacheDependencyService cacheDependencyService)
            {
                return cacheDependencyService;
            }

            return null;
        }
    }
}