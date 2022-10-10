using Dex.DistributedCache.Services;

namespace Dex.DistributedCache.Tests.Services
{
    public class CacheUserVariableKeyTest : ICacheUserVariableKey
    {
        public string GetVariableKey()
        {
            return "BCCEF0E2-9EA5-4EE3-95DB-4BDB0013A9F8";
        }
    }
}