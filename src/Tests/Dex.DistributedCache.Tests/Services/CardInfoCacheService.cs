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
                var cardList = new List<string>();
                cardList.AddRange(valueData.Select(x => x.Id.ToString()));

                partDependencies.Add(new CachePartitionedDependencies("card", cardList.Distinct().ToArray()));
            }

            if (partDependencies.Any())
            {
                await _cacheService.SetDependencyValueDataAsync(key, partDependencies.ToArray(), expiration, cancellation);
            }
        }
    }
}