using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dex.DistributedCache.Services
{
    public interface ICacheActionFilterService
    {
        Dictionary<Type, string> GetVariableKeys(Type[] cacheVariableKeys);

        Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext);

        Task<bool> TryCacheValue(string key, int expiration, Dictionary<Type, string> variableKeys, ActionExecutedContext executedContext);
    }
}