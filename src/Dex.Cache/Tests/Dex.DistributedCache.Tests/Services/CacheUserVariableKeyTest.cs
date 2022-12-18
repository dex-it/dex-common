using Dex.DistributedCache.Services;

namespace Dex.DistributedCache.Tests.Services
{
    public class CacheUserVariableKeyTest : ICacheUserVariableKeyResolver
    {
        private readonly IUserIdServiceTest _userIdService;

        public CacheUserVariableKeyTest(IUserIdServiceTest userIdService)
        {
            _userIdService = userIdService;
        }

        public string GetVariableKey()
        {
            return _userIdService.UserId.ToString();
        }
    }
}