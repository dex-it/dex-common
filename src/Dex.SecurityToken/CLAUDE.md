# Dex.SecurityToken

Генерация и валидация одноразовых security-токенов с шифрованием через ASP.NET Core DataProtection.

## DI-регистрация

```csharp
services.AddSecurityTokenProvider(config);       // IConfigurationSection "TokenProviderOptions"
services.AddSecurityTokenProvider(o => { o.ApiResource = "MyService"; });
// Регистрирует: IDataProtectionFactory (Singleton), ITokenInfoStorage (Scoped), ITokenProvider (Scoped)

// Опционально: распределённое хранилище (вместо in-memory)
services.AddDistributedTokenInfoStorage();       // заменяет ITokenInfoStorage, требует IDistributedCache
```

## Использование

Токены наследуют `BaseToken` (автоматически генерирует Id и Created).

```csharp
// Создание
var encrypted = await tokenProvider.CreateTokenAsync<CustomToken>(
    t => { t.CustomProp = value; },
    TimeSpan.FromHours(1));

// URL-safe вариант
var urlToken = await tokenProvider.CreateTokenAsUrlAsync<CustomToken>(...);

// Валидация
var data = await tokenProvider.GetTokenDataAsync<CustomToken>(encrypted, throwIfInvalid: true);

// Отметка как использованный
await tokenProvider.MarkTokenAsUsed(tokenId);
```

## Конфигурация

`TokenProviderOptions.ApiResource` (обязательно): имя API-ресурса, выпускающего токены.
Токены привязаны к ресурсу: могут быть использованы только в том ресурсе, который их выпустил.

## Исключения

`TokenExpiredException`, `TokenAlreadyActivatedException`, `TokenAlreadyExistException`, `TokenInvalidAudienceException`, `TokenInfoNotFoundException`.

## Ограничения и gotchas

- Timeout должен быть > TimeSpan.Zero (иначе ArgumentException)
- `InMemoryTokenInfoStorage` не подходит для production и распределённых систем
- Метаданные токена НЕ шифруются, шифруется только содержимое через DataProtection
- Токен можно использовать только один раз (activation предотвращает повторное использование)
