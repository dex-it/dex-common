using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dex.DistributedCache.Services
{
    internal interface ICacheActionFilterService
    {
        Guid GetUserId(bool isUserVariableKey);

        Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext);

        Task TryCacheValue(string key, int expiration, Guid userId, ActionExecutedContext executedContext);
    }
}