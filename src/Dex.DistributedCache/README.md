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

    public CardInfoCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task SetAsync(string key, CardInfo[]? valueData, int expiration, CancellationToken cancellation)
    {
        var partDependencies = new List<CachePartitionedDependencies>();

        if (valueData != null)
        {
            partDependencies.Add(new CachePartitionedDependencies("card", valueData.Select(x => x.Id.ToString()).Distinct().ToArray()));
        }

        if (partDependencies.Any())
        {
            await _cacheService.SetDependencyValueDataAsync(key, partDependencies.ToArray(), expiration, cancellation);
        }
    }
}
```

Also you need to add a service CacheUserVariableKey that implements an interface ICacheUserVariableKey : ICacheVariableKey.

Cache variability parameters - a separate cache object is created for each variable parameter.
```csharp
public interface ICacheUserVariableKey : ICacheVariableKey
{
}

public class CacheUserVariableKey : ICacheUserVariableKey
{
    private readonly IPrincipal _current;

    public CacheUserVariableKey(IPrincipal current)
    {
        _current = current;
    }

    public string GetVariableKey()
    {
        var principal = (ClaimsPrincipal)_current;
        var userIdClaim = principal.FindFirstValue(JwtClaimTypes.Subject) 
                          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? throw new AuthenticationException("Unknown UserId");
        return userIdClaim;
    }
}
```

Invalidate cache examples:
```csharp
// Invalidate by variable key
await cacheService.InvalidateByVariableKeyAsync<ICacheUserVariableKey>(values, cancellationToken);

// Invalidate by dependencies
await cacheService.InvalidateByDependenciesAsync(new[] {new CachePartitionedDependencies("card", values)}, cancellationToken);
```

It is possible to invalidate cache using middleware:
```csharp
app.UseInvalidateCacheByUserMiddleware();
```