# Dex.Configuration

Configuration-protection tooling for ASP.NET Core / .NET services. Encrypts selected JSON config values with ASP.NET Core `IDataProtector`, decrypts them transparently on application start, and ships a CLI for ahead-of-time encryption.

| Package | Purpose |
|---|---|
| `Dex.Configuration.DataProtection` | Helper around `IDataProtector` — `ProtectData` / `UnprotectData` / `ProtectEncryptedData` with a fixed (`applicationName`, `projectName`) purpose. |
| `Dex.Configuration.DataProtection.Cli` | Console tool for ops to encrypt or inspect `appsettings.*.json` values. |
| `Dex.Configuration.ProtectedJson` | `IConfigurationBuilder.AddProtectedJsonFile(...)` — JSON provider that decrypts the listed keys on load. |

---

## `Dex.Configuration.DataProtection`

Provides static `DataProtection` helpers built on top of `DataProtectionProvider.Create(keysDirectory)`:

| Method | Purpose |
|---|---|
| `ProtectData(keysDirectory, applicationName, projectName, plaintext)` | Encrypts `plaintext` and returns the protected payload. |
| `UnprotectData(keysDirectory, applicationName, projectName, protectedData)` | Decrypts a previously protected value. |
| `ProtectEncryptedData(keysDirectory, applicationName, projectName, encryptedData)` | Decrypts data protected by the in-assembly certificate (see CLI `encrypt`) and re-protects it with `IDataProtector`. |

Key facts:

* Keys are stored on the file system under `keysDirectory` (created on first use).
* The protector `purpose` is derived from `(applicationName, projectName)`. `projectName` must be a value of the `ProjectName` enum (`Template` is the only built-in member; extend the enum and add a matching `IDataProtectionPurpose` to support new projects).
* Encryption defaults to `AES-256-CBC`; key rotation defaults to **90 days** — handled by ASP.NET Core `IDataProtector` itself.
* `IDataProtector` is not intended for long-lived ciphertext — losing the key directory means losing the data.

---

## `Dex.Configuration.DataProtection.Cli`

Console tool for encrypting / decrypting configuration values.

### Commands

| Command | Build flavour | Purpose |
|---|---|---|
| `protect` | Release + Debug | Encrypts `--data` using `IDataProtector`. |
| `unprotect` | **Debug only** (`ADD_UNPROTECT`) | Decrypts `--data`. |
| `unprotect-file` | **Debug only** (`ADD_UNPROTECT`) | Loads `--file` JSON and decrypts the listed `--configuration-key` entries; `ApplicationName` is read from `ConfigurationProtectionOptions:ApplicationName`. |
| `protect-encrypted` | Release + Debug | Decrypts data produced by `encrypt` and re-protects it with `IDataProtector`. |
| `encrypt` | **Debug only** | Encrypts `--data` using the certificate embedded in the assembly. Used to hand sensitive values off to operators without exposing the `IDataProtector` keys. |

> The Release build deliberately omits `unprotect*` and `encrypt` so production binaries cannot reverse production secrets.

### Parameters

| Option | Applies to | Notes |
|---|---|---|
| `--keys-directory` | `protect`, `unprotect`, `protect-encrypted`, `unprotect-file` | Directory holding `IDataProtector` keys. |
| `--project-name` | `protect`, `unprotect`, `protect-encrypted`, `unprotect-file` | Value of the `ProjectName` enum (e.g. `Template`). |
| `--application-name` | `protect`, `unprotect`, `protect-encrypted` | Application id. For the `Template` project the supported values live in the `TemplateApplicationName` enum (e.g. `AuditWriter`). For `unprotect-file` it is taken from the JSON. |
| `--data` | `protect`, `unprotect`, `protect-encrypted`, `encrypt` | Payload to (un)protect. |
| `--file` | `unprotect-file` | Path to the JSON file to decrypt. |
| `--configuration-key` | `unprotect-file` | JSON path of a key to decrypt; repeat to decrypt several values. |

### Examples

```shell
# Encrypt a value
Dex.Configuration.DataProtection.Cli protect \
    --keys-directory ./keys \
    --project-name Template \
    --application-name AuditWriter \
    --data "rabbit_password_value"

# Decrypt selected keys of an appsettings file (Debug build)
Dex.Configuration.DataProtection.Cli unprotect-file \
    --keys-directory ./keys \
    --project-name Template \
    --file ./configs/appsettings.Production.json \
    --configuration-key "AuthorizationSettings:ApiResourceSecret" \
    --configuration-key "RabbitMqOptions:Password"

# Re-encrypt a value previously produced by `encrypt`
Dex.Configuration.DataProtection.Cli protect-encrypted \
    --keys-directory ./keys \
    --project-name Template \
    --application-name AuditWriter \
    --data "cert_encrypted_payload"
```

### Generating the embedded certificate

The `encrypt` / `protect-encrypted` flow relies on an X.509 certificate embedded in the assembly. Regenerate it with:

```powershell
New-SelfSignedCertificate `
    -Subject "CN=DataProtectionContainer" `
    -FriendlyName DataProtection `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
    -NotAfter (Get-Date).AddYears(10) -Verbose
```

---

## `Dex.Configuration.ProtectedJson`

`AddProtectedJsonFile` extends `IConfigurationBuilder` with a JSON provider that decrypts a fixed set of keys at load time.

```csharp
builder.Configuration.AddProtectedJsonFile(
    path: $"appsettings.{builder.Environment.EnvironmentName}.json",
    dataProtectorFactory: (applicationName, keysDirectory) =>
    {
        var purpose = new DataProtectionPurpose().ComputePurpose(applicationName);
        return DataProtectionProvider
            .Create(new DirectoryInfo(keysDirectory))
            .CreateProtector(purpose);
    },
    protectedKeys: new[]
    {
        "ConnectionStrings:Default",
        "ClientCredentialsOptions:ApiKey"
    },
    optional: false,
    reloadOnChange: false);

var configuration = builder.Configuration.Build();
var connectionString = configuration["ConnectionStrings:Default"]; // already decrypted
```

The JSON file **must** carry the parameters the provider needs to build the protector:

```json
{
  "ConfigurationProtectionOptions": {
    "KeysDirectory": "C:\\Keys",
    "ApplicationName": "MyApp"
  },
  "ConnectionStrings": {
    "Default": "ENCRYPTED_VALUE"
  },
  "ClientCredentialsOptions": {
    "ApiKey": "ENCRYPTED_VALUE"
  }
}
```

Missing `ConfigurationProtectionOptions:KeysDirectory` or `ConfigurationProtectionOptions:ApplicationName` raises `InvalidOperationException` on load; a `null` value for any `protectedKey` does the same.

### Delegate signature

```csharp
public delegate IDataProtector CreateDataProtectorDelegate(string applicationName, string keysDirectory);
```

The factory is called once per load and receives the application name + keys directory pulled from the JSON.

---

## Typical workflow

1. **Encrypt** secrets up front:
   ```shell
   Dex.Configuration.DataProtection.Cli protect \
       --keys-directory <keys> --project-name <Project> --application-name <App> \
       --data <secret>
   ```
2. Paste the protected value into `appsettings.<Env>.json` and list its key under `protectedKeys` of `AddProtectedJsonFile`.
3. Ship the `keys` directory to the runtime host (the application must reach the *same* `KeysDirectory` value declared in JSON).
4. The application starts, `ProtectedJsonConfigurationProvider` decrypts the listed values transparently — the rest of the code reads `IConfiguration` as usual.

---

## Breaking changes

| PR / Commit | Change |
|---|---|
| [#167](https://github.com/dex-it/dex-common/pull/167) (`dd8fd2f`) | Initial addition of `Dex.Configuration` (`DataProtection` + `DataProtection.Cli` + `ProtectedJson`) into `dex-common`. Previously distributed separately. |
