using Dex.DistributedCache.Services;

namespace Dex.DistributedCache.Tests.Services
{
    public class CacheLocaleVariableKeyTest : ICacheLocaleVariableKey
    {
        public string GetVariableKey()
        {
            return "ru";
        }
    }
}