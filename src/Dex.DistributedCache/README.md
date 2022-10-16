# Dex.DistributedCache

Distributed Cache Management - managing data caching in distributed applications.

* Caching the results of HTTP requests.
* Full support for ETag notation.
* Implemented on the basis of IDistributedCache.

Solved problems:
* The problem of updating the cache - is that there are many services that want to write or reuse the cache.
* Cache dependency problem - cached items can have many dependencies, the modification of which should raise an event for invalidation (reset)/cache update.
* Cache invalidation problem - events that affect invalidation occur throughout the system.

### Basic usage
```csharp
// For basic usage caching (without dependencies), you only need to set the [CacheActionFilter] attribute on the controller method.
// Also you can specify absolute expiration time in seconds for caching.
[HttpGet]
[CacheActionFilter(600)]
public async Task<ActionResult> Get(){...}

// Startup
var serviceCollection = new ServiceCollection()
    .AddStackExchangeRedisCache(_ => { })
    .AddDistributedCache();
```

### Advanced usage
```csharp
// For advanced usage caching (with dependencies), you need to set the [CacheActionFilter] attribute on the controller method with indication of variability keys.
[HttpGet]
[CacheActionFilter(600, typeof(ICacheUserVariableKey))]
public async Task<ActionResult> Get(){...}

// Startup
var serviceCollection = new ServiceCollection()
    .AddStackExchangeRedisCache(_ => { })
    .AddDistributedCache()
    .RegisterCacheVariableKeyService<ICacheUserVariableKey, CacheUserVariableKey>()
    .RegisterCacheDependencyService<CardInfo[], CardInfoCacheService>();
```

It is necessary to add a service CardInfoCacheService (will provide dependent data for caching) that implements an interface ICacheDependencyService<T>.

Dependencies - additional keys pointing to the cache object, allow you to reset the cache when the dependency changes.
```csharp
public class CardInfoCacheService : ICacheDependencyService<CardInfo[]>
{
    private readonly ICacheService _cacheService;
    private readonly IUserIdService _userIdService;

    public CardInfoCacheService(ICacheService cacheService, IUserIdService userIdService)
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
```

Also you need to add a service, which will receive a variable key, for example CacheUserVariableKeyResolver that implements an interface ICacheUserVariableKeyResolver : ICacheVariableKeyResolver.

Cache variability parameters - a separate cache object is created for each variable parameter.

It is possible to use our interfaces of classic cache variability keys: ICacheUserVariableKeyResolver, ICacheLocaleVariableKeyResolver.
```csharp
public class CacheUserVariableKeyResolver : ICacheUserVariableKeyResolver
{
    private readonly IUserIdService _userIdService;

    public CacheUserVariableKeyResolver(IUserIdService userIdService)
    {
        _userIdService = userIdService;
    }

    public string GetVariableKey()
    {
        return _userIdService.UserId.ToString();
    }
}
```

Invalidate cache by dependencies:
```csharp
var invalidateValues = values.Select(x => new CacheDependency(x.Id.ToString()));
await cacheService.InvalidateByDependenciesAsync(invalidateValues, CancellationToken.None);
```

It is possible to invalidate cache using middleware.
To do this, an empty special header must be added to the request: ForceInvalidateCacheByUser.
```csharp
app.UseInvalidateCacheByUserMiddleware();
```