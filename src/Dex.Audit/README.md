# Dex.Audit

End-to-end auditing for distributed .NET systems: collect domain events on clients, enrich them, ship them to a central server over MassTransit / gRPC, and persist them. Settings (which event types to record, minimum severity, etc.) are managed on the server and broadcast back to the clients.

## Packages

| Layer | Package | Purpose |
|---|---|---|
| **Client** | `Dex.Audit.Client.Abstractions` | Contracts for the client side (`IAuditWriter`, `IAuditEventConfigurator`, `AuditEventBaseInfo`, message DTOs). |
| **Client** | `Dex.Audit.Client` | `AuditWriter`, `BaseAuditEventConfigurator`, `BaseSubsystemAuditWorker`, MassTransit send endpoint helper. |
| **Server** | `Dex.Audit.Server.Abstractions` | Server-side contracts (`IAuditEventsRepository`, `IAuditSettingsRepository`, `IAuditSettingsCacheRepository`, `IAuditServerSettingsService`). |
| **Server** | `Dex.Audit.Server` | `AuditEventConsumer`, `RefreshCacheWorker`, MassTransit receive endpoint helper. |
| **Domain** | `Dex.Audit.Domain` | Audit entities (`AuditEvent`, `AuditSettings`, severity enum). |
| **Infra** | `Dex.Audit.Implementations.Common` | Shared in-memory cache repository (`SimpleAuditSettingsCacheRepository`). |
| **Infra** | `Dex.Audit.Client.Implementations` | `AddSimpleAuditClient` (in-memory cache + MassTransit settings broadcast). |
| **Infra** | `Dex.Audit.Client.Implementations.Grpc` | `AddGrpcAuditClient` (settings fetched from the server over gRPC). |
| **Infra** | `Dex.Audit.Server.Implementations` | `AddSimpleAuditServer<TDbContext>` (EF Core repositories + MassTransit settings broadcast). |
| **Infra** | `Dex.Audit.Server.Implementations.Grpc` | `AddGrpcAuditServer` (settings exposed to clients over gRPC). |
| **Infra** | `Dex.Audit.EF.Interceptors.Abstractions` | `IAuditEntity` marker interface for auditable entities. |
| **Infra** | `Dex.Audit.EF.Interceptors` | EF Core interceptors (`SaveChanges` + transaction) that audit any `IAuditEntity` change automatically. |
| **Infra** | `Dex.Audit.Logger` | `ILogger`-based capture — every log call is forwarded to the audit pipeline. |
| **Infra** | `Dex.Audit.MediatR` | MediatR pipeline behavior that audits `AuditRequest<TResponse>` commands. |

Samples live in `Tests/Dex.Audit.Sample.Client` and `Tests/Dex.Audit.Sample.Server`.

---

# Client

## `Dex.Audit.Client`

Two registration shapes — the full one lets you swap the event configurator:

```csharp
services
    .AddAuditClient<
        BaseAuditEventConfigurator,         // or your own : IAuditEventConfigurator
        YourSettingsCacheRepository,        //               : IAuditSettingsCacheRepository
        YourClientAuditSettingsService>     //               : IAuditSettingsService
    (builder.Configuration);

// Or, if BaseAuditEventConfigurator is fine:
services.AddAuditClient<YourSettingsCacheRepository, YourClientAuditSettingsService>(builder.Configuration);
```

Wire the MassTransit send endpoint inside `RegisterBus`:

```csharp
services.AddMassTransit(x =>
{
    x.RegisterBus((context, _) =>
    {
        context.AddAuditClientSendEndpoint();
    });
});
```

Optional — auto-audit `Subsystem startup`/`Subsystem shutdown` events:

```csharp
services.AddHostedService<BaseSubsystemAuditWorker>();
```

`appsettings.json`:

```json
{
  "AuditEventOptions": {
    "MinSeverityLevel": "Low",   // Low | Medium | High | Critical
    "SystemName": "YourSystemName"
  }
}
```

### Writing audit events

```csharp
public class OrderService(IAuditWriter auditWriter)
{
    public Task RecordPaymentAsync(string orderId, bool success, CancellationToken ct)
        => auditWriter.WriteAsync(
            new AuditEventBaseInfo(
                eventType:   "PaymentProcessed",
                eventObject: orderId,
                message:     "Payment confirmed by gateway",
                isSuccess:   success),
            ct);
}
```

---

# Server

## `Dex.Audit.Server`

```csharp
services
    .AddAuditServer<
        YourAuditEventsRepository,          //               : IAuditEventsRepository      (persistent store)
        YourAuditSettingsRepository,        //               : IAuditSettingsRepository    (persistent store)
        YourAuditSettingsCacheRepository,   //               : IAuditSettingsCacheRepository (cache)
        YourAuditServerSettingsService>     //               : IAuditServerSettingsService (manages settings + broadcast)
    (builder.Configuration);

services.AddMassTransit(busRegistrationConfigurator =>
{
    busRegistrationConfigurator.AddAuditServerConsumer();
    busRegistrationConfigurator.RegisterBus((context, configurator) =>
    {
        configurator.AddAuditServerReceiveEndpoint(context, enableRetry: true);
    });
});

services.AddHostedService<RefreshCacheWorker>();
```

`AddAuditServerReceiveEndpoint` accepts the batch / prefetch knobs (defaults: `prefetchCount: 600`, `messageLimit: 500`, `timeLimitSeconds: 1`, `concurrencyLimit: 1`, `retryCount: 2`, `retryIntervalSeconds: 1`).

`appsettings.json`:

```json
{
  "AuditCacheOptions": {
    "RefreshInterval": "00:00:10"
  }
}
```

`AuditEventConsumer` consumes `AuditEventMessage` and writes through `IAuditEventsRepository`. `IAuditServerSettingsService` owns the settings lifecycle and is responsible for broadcasting updates back to clients.

### Important — registering an audited event type

Every event type that should actually be recorded must be added on the server via `IAuditServerSettingsService.AddOrUpdateSettingsAsync` (or any equivalent path), and must be visible to clients via `IAuditSettingsService.GetOrGetAndUpdateSettingsAsync`. Events whose type isn't registered are filtered out by the client.

---

# Ready-made implementations

### `Dex.Audit.Client.Implementations` — MassTransit + in-memory cache

```csharp
services.AddSimpleAuditClient(builder.Configuration);
// or
services.AddSimpleAuditClient<YourAuditEventConfigurator>(builder.Configuration);
// or
services.AddSimpleAuditClient<YourAuditEventConfigurator, YourClientAuditSettingsService>(builder.Configuration);

services.AddMassTransit(x =>
{
    x.AddSimpleAuditClientConsumer();
    x.RegisterBus((context, configurator) =>
    {
        context.AddSimpleAuditClientReceiveEndpoint(configurator);
    });
});
```

Provides: `SimpleAuditSettingsCacheRepository` (`Microsoft.Extensions.Caching.Memory`), `SimpleClientAuditSettingsService` (over `Dex.MassTransit.Rabbit`).

### `Dex.Audit.Client.Implementations.Grpc`

```csharp
services.AddGrpcAuditClient<
    BaseAuditEventConfigurator,
    YourSettingsCacheRepository>(
    builder.Configuration,
    configureClient: () =>  // optional HttpMessageHandler factory
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return handler;
    });
```

```json
{ "AuditGrpcOptions": { "ServerAddress": "http://localhost:7240" } }
```

Replaces the MassTransit settings transport with `GrpcAuditSettingsService` + `GrpcAuditBackgroundWorker`. Audit *events* are still shipped via MassTransit.

### `Dex.Audit.Server.Implementations`

```csharp
services.AddSimpleAuditServer<YourDbContext>(builder.Configuration);
// or override the settings service:
services.AddSimpleAuditServer<YourDbContext, YourAuditServerSettingsService>(builder.Configuration);

public class YourDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddAuditEntities();
    }
}
```

Provides EF Core repositories (`SimpleAuditEventsRepository<TDbContext>`, `SimpleAuditSettingsRepository<TDbContext>`), `SimpleAuditSettingsCacheRepository`, and `SimpleAuditServerSettingsService` (broadcasts via `Dex.MassTransit.Rabbit`).

### `Dex.Audit.Server.Implementations.Grpc`

```csharp
services
    .AddGrpcAuditServer<
        YourAuditEventsRepository,
        YourAuditSettingsRepository,
        YourAuditSettingsCacheRepository>(builder.Configuration);
```

Registers a gRPC server endpoint and `AuditSettingsServiceWithGrpcNotifier` to push settings to subscribed clients.

---

# Auxiliary modules

## `Dex.Audit.EF.Interceptors`

Auto-audits every change to entities implementing `IAuditEntity`:

```csharp
public class Order : IAuditEntity { … }

services.AddAuditInterceptors<InterceptionAndSendingEntriesService>();
// or your own : IInterceptionAndSendingEntriesService

services.AddDbContext<YourDbContext>((sp, opt) =>
{
    opt.UseNpgsql(connectionString);
    opt.AddInterceptors(
        sp.GetRequiredService<IAuditSaveChangesInterceptor>(),
        sp.GetRequiredService<IAuditDbTransactionInterceptor>());
});
```

Any `SaveChangesAsync` or explicit transaction commit involving an `IAuditEntity` produces an audit event.

## `Dex.Audit.Logger`

Capture log calls as audit events:

```csharp
services.AddLogging(b => b.AddAuditLogger());
```

```json
{ "AuditLoggerOptions": { "ReadEventsInterval": "00:00:10" } }
```

Level-specific helpers (`LogAuditInformation`, `LogAuditWarning`, `LogAuditError`, `LogAuditCritical`, `LogAuditDebug`, `LogAuditTrace`) and a generic one:

```csharp
logger.LogAuditInformation(eventType: "UserLogin", message: "User {Id} signed in", userId);
logger.LogAudit(LogLevel.Warning, eventType: "RateLimit", message: "throttled {Endpoint}", endpoint);
```

A hosted `AuditLoggerReader` drains the in-memory queue and forwards entries to `IAuditWriter` on the configured interval.

## `Dex.Audit.MediatR`

Audits MediatR commands automatically:

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddPipelineAuditBehavior();
});

public class CreateOrderCommand : AuditRequest<CreateOrderResponse>
{
    public override string EventType   => "OrderCreated";
    public override string EventObject => OrderId;
    public override string Message     => "Order accepted by the API";
    public string OrderId { get; init; } = "";
}

public class CreateOrderResponse : IAuditResponse { public bool IsSuccess { get; init; } }
```

The `AuditBehavior<,>` pipeline behavior emits an audit event for each request, marking success based on `IAuditResponse.IsSuccess`.

---

# Breaking changes

| Version / PR | Change |
|---|---|
| [#182](https://github.com/dex-it/dex-common/pull/182) (`adb38d0`) | Internal namespaces tidied — update `using`s if you were importing internal types. NuGet metadata refreshed. |
| [#169](https://github.com/dex-it/dex-common/pull/169) (`99a1cc3`) | `Dex.Audit.*` now references `Dex.MassTransit.Rabbit` directly — the Rabbit extension `RegisterBus`/`RegisterSendEndPoint` API is required for `AddAuditClientSendEndpoint`/`AddAuditServerReceiveEndpoint`. |
| [#164](https://github.com/dex-it/dex-common/pull/164) (`8e5e448`) | Packaging settings reworked — version pinning across `Dex.Audit.*` packages is now expected. Bump all of them together. |
| [#151](https://github.com/dex-it/dex-common/pull/151) (`01bee77`) | Initial merge of `Dex.Audit` into `main` — the previous separate-repo layout is gone. |

> Note: the previously-mentioned `Dex.Audit.EF.NpgSql` package no longer exists in this repository. Configure your own PostgreSQL provider on the `DbContext` used with `AddSimpleAuditServer<TDbContext>`.
