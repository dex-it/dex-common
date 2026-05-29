# Dex.Cache (Dex.DistributedCache)

Кэш HTTP-ответов поверх `IDistributedCache` с ETag, зависимостями и инвалидацией.

## DI-регистрация

```csharp
services.AddStackExchangeRedisCache(_ => { });  // ОБЯЗАТЕЛЬНО первым
services.AddDistributedCache();                  // ICacheService, ICacheManagementService, etc.
```

## Использование

CacheActionFilter на контроллере:
```csharp
[CacheActionFilter(600)]                                          // 10 мин
[CacheActionFilter(600, typeof(ICacheUserVariableKeyResolver))]   // per-user
```

Инвалидация через middleware:
```csharp
app.UseInvalidateCacheByUserMiddleware();  // заголовок ForceInvalidateCacheByUser
```

Программная инвалидация:
```csharp
await cacheService.InvalidateByDependenciesAsync(dependencies);
```

## Ключевые концепции

- Ключ кэша: MD5 от URL + variable keys (user, locale и т.д.)
- Хранение раздельное: `dc:meta:{{hash}}` (заголовки) и `dc:value:{{hash}}` (тело)
- Зависимости: `dc:dep:{{value}}` хранит список зависимых ключей кэша
- Variable keys: `ICacheUserVariableKeyResolver`, `ICacheLocaleVariableKeyResolver`
- Кастомные зависимости: реализовать `ICacheDependencyService<T>`

## Ограничения и gotchas

- `AddStackExchangeRedisCache` ОБЯЗАТЕЛЬНО регистрировать до `AddDistributedCache()`
- Зависимости не устанавливаются автоматически: нужна реализация `ICacheDependencyService<T>`
- Variable keys требуют и регистрации resolver-а, и указания типа в атрибуте фильтра
- MetaInfo и ValueData хранятся раздельно: при инвалидации удаляются оба
