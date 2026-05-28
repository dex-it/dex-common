# Dex.ResponseSigning

Server-side response signing and client-side verification for ASP.NET Core. Signs a successful (HTTP 200) MVC action result as a **detached-style JWS** (`header.payload.signature`) using the certificate embedded in the package, then transparently verifies and unwraps the payload on the client.

Goal: prevent silent service substitution / response tampering between trusted internal services. The signature does **not** provide confidentiality — only authenticity / integrity.

| Side | Component | Purpose |
|---|---|---|
| Server | `AddResponseSigning` + `[SignResponseFilter]` | Wraps the action's response object into a JWS string. |
| Client | `AddResponseVerifying` + `AddResponseVerifyingHandler` | Validates the JWS, replaces the response body with the original payload, fails the call on tamper. |

---

## Configuration

```jsonc
{
  "ResponseSigningOptions": {
    "DefaultPassword": "wNx!a2*ThM",
    "Algorithm": "RS256"          // optional, default RS256
  }
}
```

* `DefaultPassword` — password for the embedded PFX certificate (the same on both sides).
* `Algorithm` — JWS `alg` header value. Currently the implementation is hard-wired to RSA (`SHA-256` + `PKCS#1` padding); change at your own risk.

---

## Server — signing a response

```csharp
builder.Services.AddResponseSigning(builder.Configuration);
```

Apply the filter to any controller or action whose response must be signed:

```csharp
[HttpGet]
[SignResponseFilter]
public ActionResult<object> Get() => Ok(new
{
    TestNum    = 180,
    TestString = "Test",
    TestDate   = DateTime.Now
});
```

Behaviour:

* Only `ObjectResult` with `StatusCode == 200` is signed; everything else passes through untouched.
* `null` payloads are rejected (`InvalidOperationException("Cannot sign empty payload.")`).
* The wire format is a compact JWS string `base64url(header).base64url(payload).base64url(signature)`. The body MIME type stays whatever the action returned.

Custom JSON shape:

```csharp
services.AddSigningDataSerializationOptions(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
```

If you skip this call, the package uses its own default `JsonSerializerOptions` instance — **not** ASP.NET Core's MVC options. The same options must be configured on both sides, otherwise verification will succeed but deserialization on the client will read different field names.

---

## Client — verifying a response

```csharp
builder.Services.AddResponseVerifying(builder.Configuration);

builder.Services
    .AddHttpClient("TestClient", c => c.BaseAddress = new Uri("http://localhost:5678"))
    .AddResponseVerifyingHandler();

builder.Services
    .AddRefitClient<IRespondentApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5678"))
    .AddResponseVerifyingHandler();
```

`SignatureVerificationHandler` runs on every successful response (`IsSuccessStatusCode == true`):

1. Reads the body as a string.
2. Verifies the JWS signature with the embedded certificate's public key.
3. Replaces `response.Content` with the **original payload** (the same bytes the server passed to the filter).

On verification failure the handler throws `InvalidOperationException("Verification failed.")` — the call propagates the exception instead of returning silently.

Non-2xx responses are passed through verbatim (errors are never signed).

---

## Public API surface

| Type / Method | Purpose |
|---|---|
| `AddResponseSigning(IConfiguration)` | Registers signing services and `ResponseSigningOptions`. |
| `AddResponseVerifying(IConfiguration)` | Registers verification services and the message handler. |
| `AddSigningDataSerializationOptions(JsonSerializerOptions)` | Overrides the JSON serializer used for the signed payload. |
| `[SignResponseFilter]` | MVC action filter that signs successful `ObjectResult`s. |
| `AddResponseVerifyingHandler()` | `IHttpClientBuilder` extension — adds `SignatureVerificationHandler` to the pipeline. |
| `IJwsSignatureService.SignData(object)` | Lower-level entry — wraps a payload as a JWS string. |
| `IJwsParsingService.ParseJws<T>(string)` / `GetSerializedResponse(string)` | Lower-level entry — verifies + extracts the JWS payload. |

---

## Notes

* Keys are loaded from a PFX embedded in `Dex.ResponseSigning`. Both signer and verifier must reference the same package version, and `ResponseSigningOptions.DefaultPassword` must match on both ends.
* Only the response **body** is covered. Headers, status, and route are not.
* The implementation is RSA-specific — `SigningConstants.HashAlgorithmName = SHA256`, `Padding = PKCS#1`. The `Algorithm` option only changes the value advertised in the JWS header.
* `SignatureVerificationHandler` always reads the body fully into memory before substituting it — not suitable for streaming responses.

---

## Breaking changes

| PR / Commit | Change |
|---|---|
| [#168](https://github.com/dex-it/dex-common/pull/168) (`dd8fd2f`) | Initial addition of `Dex.Response.Signing` to `dex-common`. |
