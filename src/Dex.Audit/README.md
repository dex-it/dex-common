# Dex.Audit

### Provides functionality for auditing your system.

# Dex.Audit.Client

### Provides functionality for auditing to events, enriching them and sending them to the listening server.

### Implementation

In code
```csharp
services
    .AddAuditClient<
    BaseAuditEventConfigurator, // or can be used your inherited and overriden realization of BaseAuditEventConfigurator class  
    YourAuditSettingsCacheRepository, // must be inherited and implemented from interface IAuditSettingsCacheRepository
    YourClientAuditSettingsService> //  must be inherited and implemented from interface IAuditSettingsService
    (builder.Configuration);

services
    .AddMassTransit(x =>
    {
        ...            
        x.RegisterBus((context, configurator) =>
        {
            ...
            context.AddAuditClientSendEndpoint();
            ...
        });
    });
```

In appsettings
```jsim
{
  "AuditEventOptions": {
    "MinSeverityLevel": "Low", // Low, Medium, High, Critical
    "SystemName": "YourSystemName"
  }
}
```

### Basic usage

```csharp
class YourClass
{
    private IAuditWriter _auditWriter;

    async Task YourMethdod(
        string eventType,
        string? eventObject,
        string? message,
        bool success,
        CancellationToken, cancellationToken)
    {
        await auditWriter
            .WriteAsync(
            new AuditEventBaseInfo(
            eventType,
            eventObject,
            message,
            success),
            cancellationToken);
    }
}
``` 

# Dex.Audit.Server

### Provides an auditing server functionality, which means receiving and storing events from clients, sending updated settings to clients and storing event settings.

### Implementation

In code
```csharp
class YourContext : DbContext
{
    ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ...
        modelBuilder.AddAuditEntities();
    }
}

services
    .AddAuditServer<
    YourAuditEventsRepository, // must be inherited and implemented from interface IAuditEventsRepository (Preferably persistente store)
    YourAuditSettingsRepository, // must be inherited and implemented from interface IAuditSettingsRepository (Preferably persistente store)
    YourAuditSettingsCacheRepository, // must be inherited and implemented from interface IAuditSettingsCacheRepository (Preferably cache store)
    YourAuditServerSettingsService> // must be inherited and implemented from interface IAuditServerSettingsService
    (builder.Configuration);

services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddAuditServerConsumer();

            busRegistrationConfigurator.RegisterBus((context, configurator) =>
            {
                configurator.AddAuditServerReceiveEndpoint(context, true);
            });
        });
```

In appsettings
```jsim
{
  "AuditCacheOptions": {
    "RefreshInterval": "00:00:10"
  }
}
```

### Basic usage
`AuditEventConsumer` will do the work of consuming `AuditEventMessage` from clients. It uses IAuditEventsRepository and IAuditSettingsCacheRepository.

`IAuditServerSettingsService` must have logic to manage settings in your system and sending updated settings to clients.

# IMPORTANT for Audit Client and Server

Any event, that must be audited, should be added to AuditSettings with `IAuditServerSettingsService.AddOrUpdateSettingsAsync` (or another way) on server and must be available timely from `IAuditSettingsService.GetOrGetAndUpdateSettingsAsync` on client.

# Dex.Audit.EF.NpgSql

### Provides ef npgsql configuration for audit entities.

# Dex.Audit.EF.Interceptors.Abstractions

### Provides interface IAuditEntity  for Dex.Audit.EF.Interceptors library.

# Dex.Audit.EF.Interceptors

### Provides ready-made functionality for auditing operations on DbContext entities.

### Implementation

In code
```csharp
services
    .AddAuditInterceptors<
    InterceptionAndSendingEntriesService // or can be used your inherited and overriden realization of InterceptionAndSendingEntriesService class
    >();

services.AddDbContext<YourDbContext>((serviceProvider, options) =>
        {
            ...
            options.AddInterceptors(
                serviceProvider.GetRequiredService<IAuditSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<IAuditDbTransactionInterceptor>());
        });

public class YourAuditableEntity : IAuditEntity
```

### Basic usage
Any operation with IAuditEntity in YourDbContext on SaveChanges and/in Transaction will be audited.


# Dex.Audit.Logger

### Provides ready-made functionality for auditing logs.

### Implementation

In code
```csharp
services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger());
```

In appsettings
```jsim
{
  "AuditLoggerOptions": {
    "ReadEventsInterval": "00:00:10"
  }
}
```

### Basic usage
```csharp
logger.LogAudit(LogLevel, LogEventName, LogMessage, LogMessageParams);
logger.LogAuditInfo(LogEventName, LogMessage, LogMessageParams);
```


# Dex.Audit.MediatR

### Provides ready-made functionality for auditing commands and responses in MediatR Pipeline.

### Implementation

In code
```csharp
services.AddMediatR(configuration =>
        {
            ...
            configuration.AddPipelineAuditBehavior();
        });

public class YourAuditableCommand : AuditRequest<YourAuditableResponse>

public class YourAuditableResponse : IAuditResponse;
```

### Basic usage
Any command, which inherits AuditRequest and response, which inherits IAuditResponse will be audited.


# Audit Grpc libraries

### These libraries provide a ready-made communication capability between the client and the server.

# Dex.Audit.Client.Grpc

### Implementation

In code
```csharp
services.AddGrpcAuditClient<
    BaseAuditEventConfigurator,  // or can be used your inherited and overriden realization of BaseAuditEventConfigurator class
    YourAuditSettingsCacheRepository>(  // must be inherited and implemented from interface IAuditSettingsCacheRepository
        builder.Configuration,
        () => // Configuration of HttpMessageHandler must be provided or will be used default
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });
```

In appsettings
```jsim
{
  "AuditGrpcOptions": {
    "ServerAddress": "http://localhost:7240"
  }
}
```

Another implementation and usage is similar to Audit Client. IAuditSettingsService is already implemented.

# Dex.Audit.Server.Grpc

### Implementation

In code
```csharp
services
    .AddGrpcAuditServer<
    YourAuditEventsRepository, // must be inherited and implemented from interface IAuditEventsRepository (Preferably persistente store)
    YourAuditSettingsRepository, // must be inherited and implemented from interface IAuditSettingsRepository (Preferably persistente store)
    YourAuditSettingsCacheRepository, // must be inherited and implemented from interface IAuditSettingsCacheRepository (Preferably cache store)
    >(builder.Configuration);
```

Another implementation and usage is similar to Audit Server. IAuditServerSettingsService is already implemented.

# Samples

### Samples of Audit Client and Audit Server lays in Tests folder.

Dex.Audit.Sample.Client
Dex.Audit.Sample.Server