# Dex.DistributedCache

Distributed HTTP-response cache for ASP.NET Core on top of `IDistributedCache` (Redis, in-memory, etc.). Adds:

* `[CacheActionFilter]` attribute that caches an MVC action's full response.
* Full ETag negotiation (`If-None-Match` → `304 Not Modified`).
* Variable cache keys (per-user, per-locale, per-anything via `ICacheVariableKeyResolver`).
* Dependency-driven invalidation (one cache entry can be invalidated through any of its registered dependency keys).

### Problems it solves

* **Stale cache across services** — multiple services share the same backing store via `IDistributedCache`.
* **Cache dependencies** — a single cached entry can be linked to many domain keys; modifying any one of them invalidates all dependent entries.
* **Cross-cutting invalidation** — invalidation events can originate anywhere in the system, not only at the writer of the entry.

---

### Basic usage

```csharp
// Controller — cache the response for 600 seconds, no variability:
[HttpGet]
[CacheActionFilter(600)]
public async Task<ActionResult<Product[]>> Get() { … }
```

```csharp
// Startup / Program.cs
services
    .AddStackExchangeRedisCache(opt => opt.Configuration = "localhost:6379")
    .AddDistributedCache();
```

If `[CacheActionFilter]` is applied without arguments, the default expiration is **3600 seconds** (1 hour).

---

### Advanced usage — variability + dependencies

```csharp
// Per-user response, invalidated on changes to UserId or CardInfo.Id
[HttpGet]
[CacheActionFilter(600, typeof(ICacheUserVariableKeyResolver))]
public async Task<ActionResult<CardInfo[]>> GetCards() { … }
```

```csharp
services
    .AddStackExchangeRedisCache(opt => opt.Configuration = "localhost:6379")
    .AddDistributedCache()
    .RegisterCacheVariableKeyResolver<ICacheUserVariableKeyResolver, CacheUserVariableKeyResolver>()
    .RegisterCacheDependencyService<CardInfo[], CardInfoCacheService>();
```

#### Variable key resolver

A resolver returns a string that varies the cache key (e.g. the current user id). Built-in marker interfaces: `ICacheUserVariableKeyResolver`, `ICacheLocaleVariableKeyResolver`. You can declare your own by inheriting `ICacheVariableKeyResolver`.

```csharp
public class CacheUserVariableKeyResolver(IUserIdService userIdService) : ICacheUserVariableKeyResolver
{
    public string GetVariableKey() => userIdService.UserId.ToString();
}
```

#### Dependency service

For a given action result type, registers extra dependency keys on the cache entry. When any of those keys is later invalidated, the entry is dropped.

```csharp
public class CardInfoCacheService(ICacheService cacheService, IUserIdService userIdService)
    : ICacheDependencyService<CardInfo[]>
{
    public async Task SetAsync(string key, CardInfo[]? valueData, int expiration, CancellationToken ct)
    {
        var deps = new List<CacheDependency>
        {
            new(userIdService.UserId.ToString())
        };

        if (valueData is not null)
            deps.AddRange(valueData.Select(x => new CacheDependency(x.Id.ToString())).Distinct());

        if (deps.Count > 0)
            await cacheService.SetDependencyValueDataAsync(key, deps, expiration, ct);
    }
}
```

---

### Invalidation

Directly, from anywhere in the code:

```csharp
var deps = updatedCards.Select(c => new CacheDependency(c.Id.ToString()));
await cacheService.InvalidateByDependenciesAsync(deps, ct);
```

Via middleware — a special request header `ForceInvalidateCacheByUser` invalidates all entries dependent on the current user's `ICacheUserVariableKeyResolver` value:

```csharp
app.UseInvalidateCacheByUserMiddleware();
```

The middleware silently logs and swallows exceptions (cache errors never break the request pipeline) and requires `ICacheUserVariableKeyResolver` to be registered.

---

### Public API surface

| Type | Purpose |
|---|---|
| `[CacheActionFilter(int expiration = 3600, params Type[] cacheVariableKeyResolvers)]` | Caches the full action response with the listed variability resolvers. |
| `ICacheService` | `SetDependencyValueDataAsync`, `InvalidateByDependenciesAsync`. |
| `ICacheVariableKeyResolver` | Implement to add a new variability axis. Marker subtypes: `ICacheUserVariableKeyResolver`, `ICacheLocaleVariableKeyResolver`. |
| `ICacheDependencyService<TValue>` | Computes the dependency keys for an action result of type `TValue`. |
| `CacheDependency(string Value)` | Record used as a dependency key. |
| `AddDistributedCache()` | Registers core services on top of an already-registered `IDistributedCache`. |
| `RegisterCacheVariableKeyResolver<TInterface, TService>()` | Registers a variability resolver. |
| `RegisterCacheDependencyService<TValue, TService>()` | Registers a dependency producer for a result type. |
| `UseInvalidateCacheByUserMiddleware()` | Enables the `ForceInvalidateCacheByUser` header. |

---

### Notes

* `AddDistributedCache()` only wires up the cache services; the underlying `IDistributedCache` provider (Redis, in-memory, SQL Server) must be registered separately.
* Cache keys are MD5 hashes of `(request URL, variable keys)` and prefixed with `dc:` plus a kind suffix (`meta` / `value` / `dep`). Treat the Redis namespace as owned by this package.
* All cache writes use `AbsoluteExpirationRelativeToNow = expiration` seconds — there is no sliding expiration.
