using System;

namespace Dex.DistributedCache.Services
{
    public interface ICacheVariableKeyResolverFactory
    {
        ICacheVariableKeyResolver? GetCacheVariableKeyResolverService(Type type);
    }
}