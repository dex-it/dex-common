using Dex.DistributedCache.Services;

namespace Dex.DistributedCache.Tests.Services
{
    public class CacheLocaleVariableKeyTest : ICacheLocaleVariableKeyResolver
    {
        public string GetVariableKey()
        {
            return "ru";
        }
    }
}