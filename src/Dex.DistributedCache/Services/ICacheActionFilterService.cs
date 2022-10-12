﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dex.DistributedCache.Services
{
    public interface ICacheActionFilterService
    {
        IDictionary<Type, string> GetVariableKeys(IEnumerable<Type> cacheVariableKeyResolvers);

        Task<bool> CheckExistingCacheValue(string key, ActionExecutingContext executingContext);

        Task<bool> TryCacheValue(string key, int expiration, ActionExecutedContext executedContext);
    }
}