using System;

namespace Dex.DistributedCache.Services
{
    public interface ICacheDependencyFactory
    {
        ICacheDependencyService? GetCacheDependencyService(Type type);
    }
}