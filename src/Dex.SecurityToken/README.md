# Dex.SecurityToken

A small toolkit for issuing short-lived, one-shot **security tokens** (password reset, email confirmation, magic links, signed callbacks, etc.). A token is your typed payload, serialized to JSON, protected by ASP.NET Core `IDataProtector`, then stored alongside a server-side `TokenInfo` record so it can be expired and burned after use.

| Package | Purpose |
|---|---|
| `Dex.SecurityTokenProvider` | `ITokenProvider`, `IDataProtectionFactory`, in-memory `ITokenInfoStorage`. Core API. |
| `Dex.SecurityToken.DistributedStorage` | `ITokenInfoStorage` implementation backed by `IDistributedCache` (Redis, in-memory, etc.). |

> The provider depends on `Microsoft.AspNetCore.DataProtection`. You must register `IDataProtectionProvider` yourself (key persistence, key ring purpose, encryption algorithm) — `Dex.SecurityToken` neither configures nor stores keys.

---

## Concepts

* **`BaseToken`** — abstract record-like class with `Id`, `Created`, `Expired`, `Audience`. Inherit from it and add your own data.
* **`TokenInfo`** — server-side state (`Id`, `Expired`, `Activated`). Lives in `ITokenInfoStorage`.
* **Encoded token** — the public-facing string returned to the user. It is `IDataProtector.Protect(JsonSerializer.Serialize(token))`.
* **Audience** — the `ApiResource` value from `TokenProviderOptions`. Validation rejects tokens issued for a different `Audience`.

The token's **payload** travels in the encoded string. The **state** (was it activated? is it expired?) is loaded from `ITokenInfoStorage` on every read.

---

## Configuration

```jsonc
{
  "TokenProviderOptions": {
    "ApiResource": "my-api"
  }
}
```

Register the provider:

```csharp
services.AddDataProtection()                       // YOUR call — required.
    .PersistKeysToFileSystem(new DirectoryInfo("/var/keys/myapi"));

services.AddSecurityTokenProvider(builder.Configuration.GetSection("TokenProviderOptions"));
// or
services.AddSecurityTokenProvider(opt => opt.ApiResource = "my-api");
```

Pick a storage:

```csharp
// In-process — `AddSecurityTokenProvider` registers InMemoryTokenInfoStorage by default;
// fine for tests/single-instance demos, NOT for production (state is lost on restart).

// Distributed (recommended for production):
services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
services.AddDistributedTokenInfoStorage();         // overrides the in-memory storage
```

---

## Issuing a token

```csharp
public sealed class PasswordResetToken : BaseToken
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = "";
}

public class PasswordResetService(ITokenProvider tokens)
{
    public Task<string> CreateLinkAsync(Guid userId, string email, CancellationToken ct) =>
        tokens.CreateTokenAsUrlAsync<PasswordResetToken>(
            t =>
            {
                t = t with { };          // BaseToken sets Id/Created; Audience/Expired are set by the provider
                // populate writable fields if your token uses `init`-only properties through the action
            },
            timeout: TimeSpan.FromHours(2),
            ct);
}
```

`CreateTokenAsync` / `CreateTokenAsUrlAsync`:
* require `timeout > 0`;
* set `Audience = TokenProviderOptions.ApiResource` and `Expired = UtcNow + timeout`;
* run your `Action<T>` to fill custom fields;
* persist a `TokenInfo` record;
* return the data-protector-encoded string (the URL variant additionally `Uri.EscapeDataString`s it).

---

## Reading and consuming a token

```csharp
public class PasswordResetController(ITokenProvider tokens)
{
    [HttpPost("reset/{encoded}")]
    public async Task<IActionResult> Reset(string encoded, ResetDto dto, CancellationToken ct)
    {
        var token = await tokens.GetTokenDataFromUrlAsync<PasswordResetToken>(encoded, ct: ct);
        // ... do the work ...
        await tokens.MarkTokenAsUsed(token.Id);     // burn it
        return Ok();
    }
}
```

`GetTokenDataAsync` / `GetTokenDataFromUrlAsync`:

| Check | Behaviour when `throwIfInvalid: true` (default) |
|---|---|
| `TokenInfo` missing in storage | `TokenInfoNotFoundException` |
| `TokenInfo.Expired ≤ UtcNow` | `TokenExpiredException` |
| `TokenInfo.Activated` | `TokenAlreadyActivatedException` |
| `token.Audience ≠ ApiResource` | `TokenInvalidAudienceException` |

With `throwIfInvalid: false` the validations are skipped and the deserialized token is returned regardless — useful for diagnostic endpoints. `MarkTokenAsUsed` flips `TokenInfo.Activated`; the next read will then throw.

---

## Public API surface

| Type | Purpose |
|---|---|
| `ITokenProvider` | `CreateTokenAsync`, `CreateTokenAsUrlAsync`, `GetTokenDataAsync`, `GetTokenDataFromUrlAsync`, `MarkTokenAsUsed`. |
| `ITokenInfoStorage` | Persistence contract — `GetTokenInfoAsync`, `SaveTokenInfoAsync`, `SetActivatedAsync`. Implementations: `InMemoryTokenInfoStorage` (default), `DistributedTokenStorageProvider`. |
| `IDataProtectionFactory` | Caches one `IDataProtector` per `purpose` string. Built on top of `IDataProtectionProvider`. |
| `BaseToken` | Required base for your token DTO. |
| `TokenInfo` | Server-side state record (`Id`, `Expired`, `Activated`). |
| `TokenProviderOptions` | `ApiResource` (required). |
| Exceptions | `TokenInfoNotFoundException`, `TokenExpiredException`, `TokenAlreadyActivatedException`, `TokenAlreadyExistException`, `TokenInvalidAudienceException`. |
| `AddSecurityTokenProvider(...)` | Registers the core services. |
| `AddDistributedTokenInfoStorage()` | Switches `ITokenInfoStorage` to the `IDistributedCache`-backed implementation. Call **after** `AddSecurityTokenProvider`. |

---

## Notes

* `IDataProtector` is intended for **short-lived** ciphertext. The key ring rotates (90 days by default) and old keys may be evicted — tokens that outlive a rotation can become undecryptable. Keep `timeout` aligned with your key-ring retention.
* `InMemoryTokenInfoStorage` is **process-local**. In a multi-instance deployment a token issued by node A would be invisible to node B — use `AddDistributedTokenInfoStorage` (or your own `ITokenInfoStorage`) in production.
* `DistributedTokenStorageProvider` serializes `TokenInfo` as JSON via `IDistributedCacheTypedClient`. The cache key is the token id. Entries get `AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(token.Expired.Ticks)` — a long-lived expiration computed from the absolute timestamp.
* `CreateTokenAsUrlAsync` returns a URL-escaped string. `GetTokenDataFromUrlAsync` un-escapes it before decryption.

---

## Breaking changes

| PR / Commit | Change |
|---|---|
| [#82](https://github.com/dex-it/dex-common/pull/82) (`c5a09d1`) | Review pass on the security-token API. Audit `ITokenProvider` consumers if you pin to an old major. |
| [#60](https://github.com/dex-it/dex-common/pull/60) (`9adc32d`) | `DataProtectionFactory` now requires `IDataProtectionProvider` in DI (previously called `DataProtectionProvider.Create` internally). Bug fix — only the DI provider honours your host's key store and `AddDataProtection` configuration. Make sure your host registers `services.AddDataProtection()...`. |
| [#47](https://github.com/dex-it/dex-common/pull/47) (`0f33272`) | Initial introduction of `Dex.SecurityTokenProvider` and `Dex.SecurityToken.DistributedStorage`. |
