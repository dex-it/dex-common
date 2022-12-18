using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Models;
using Dex.DistributedCache.Services;
using Dex.DistributedCache.Tests.Models;

namespace Dex.DistributedCache.Tests.Services
{
    public class CardInfoCacheService : ICacheDependencyService<CardInfo[]>
    {
        private readonly ICacheService _cacheService;
        private readonly IUserIdServiceTest _userIdService;

        public CardInfoCacheService(ICacheService cacheService, IUserIdServiceTest userIdService)
        {
            _cacheService = cacheService;
            _userIdService = userIdService;
        }

        public async Task SetAsync(string key, CardInfo[]? valueData, int expiration, CancellationToken cancellation)
        {
            var dependencies = new List<CacheDependency>();
            dependencies.Add(new CacheDependency(_userIdService.UserId.ToString()));

            if (valueData != null)
            {
                var cardList = valueData.Select(x => new CacheDependency(x.Id.ToString())).Distinct();
                dependencies.AddRange(cardList);
            }

            if (dependencies.Any())
            {
                await _cacheService.SetDependencyValueDataAsync(key, dependencies, expiration, cancellation);
            }
        }
    }
}