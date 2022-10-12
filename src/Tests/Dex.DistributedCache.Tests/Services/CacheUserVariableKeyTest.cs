using Dex.DistributedCache.Services;

namespace Dex.DistributedCache.Tests.Services
{
    public class CacheUserVariableKeyTest : ICacheUserVariableKeyResolver
    {
        public string GetVariableKey()
        {
            return "54a1e183-cbee-4fce-bdce-1b6d9e257471";
        }
    }
}