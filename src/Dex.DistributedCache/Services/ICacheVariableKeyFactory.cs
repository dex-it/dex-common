using System;

namespace Dex.DistributedCache.Services
{
    public interface ICacheVariableKeyFactory
    {
        ICacheVariableKey? GetCacheVariableKeyService(Type type);
    }
}