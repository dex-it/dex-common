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

        public CardInfoCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task SetAsync(string key, CardInfo[]? valueData, int expiration, CancellationToken cancellation)
        {
            var partDependencies = new List<CachePartitionedDependencies>();

            if (valueData != null)
            {
                var cardList = valueData.Select(x => x.Id).Distinct().Select(x => x.ToString()).ToArray();
                partDependencies.Add(new CachePartitionedDependencies("card", cardList));
            }

            if (partDependencies.Any())
            {
                await _cacheService.SetDependencyValueDataAsync(key, partDependencies, expiration, cancellation);
            }
        }
    }
}