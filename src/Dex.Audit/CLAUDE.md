# Dex.Audit

Клиент-серверная система аудита. Транспорт: MassTransit (RabbitMQ) или gRPC.

## Структура solution (16 проектов)

Domain (1), Client (+ Abstractions), Server (+ Abstractions), Infrastructure: Implementations, Implementations.Grpc, EF.Interceptors (+ Abstractions), Implementations.Common, Logger, MediatR, Server.Implementations (+ Grpc). Samples: Client, Server, Shared.

## DB-модели

`AuditEvent`: Id (Guid), EventType, Source (owned VO: Device, UserDetails, AddressInfo), Destination (owned VO), EventObject, Message, IsSuccess. EF: `modelBuilder.AddAuditEntities()`.
`AuditSettings`: Id (Guid), EventType (unique), SeverityLevel (Low=0, Medium=1, High=2, Critical=3).

## Event flow и severity-фильтрация

`IAuditWriter.WriteAsync(AuditEventBaseInfo, token)`. `AuditEventBaseInfo`: EventType, EventObject?, Message?, IsSuccess.

Pipeline: WriteAsync -> fetch settings по EventType -> severity check -> enrich via `IAuditEventConfigurator` -> publish via `IAuditOutputProvider`.

Двойная фильтрация severity:
- Клиент (`AuditWriter`): если `setting.SeverityLevel < MinSeverityLevel` -- событие НЕ отправляется
- Сервер (`AuditEventConsumer`): если `setting.SeverityLevel >= message.SourceMinSeverityLevel` -- событие сохраняется

Обогащение: `BaseAuditEventConfigurator` (virtual methods): `GetSourceAddressAsync()` (IP, MAC, DNS, host), `GetDeviceInfoAsync()` (Assembly, SystemName), `GetUserDetailsAsync()` (пустой по умолчанию, переопределить для auth-контекста).

## Серверная batch-обработка

MassTransit `Batch<AuditEventMessage>`: MessageLimit=500, TimeLimit=1s, PrefetchCount=600, ConcurrencyLimit=1. Параметры настраиваются в `AddAuditServerReceiveEndpoint(context, enableRetry, prefetchCount, messageLimit, timeLimitSeconds, concurrencyLimit, retryCount, retryIntervalSeconds)`.

## Settings sync

MassTransit: сервер publish `AuditSettingsDto`, клиент `SimpleAuditSettingsUpdatedConsumer` обновляет кэш. `RefreshCacheWorker` (server BackgroundService): каждые `RefreshInterval` перечитывает DB.
gRPC: server-side streaming `GetSettingsStream`, клиент `GrpcAuditBackgroundWorker` читает стрим в реальном времени.
Кэш: `IMemoryCache`, без TTL (lifetime = приложение). HTTP-fallback при cache miss: GET к `AuditServerSettingsAddress`.

## EF Core Interceptors

Сущности: маркерный интерфейс `IAuditEntity` (без методов). `IInterceptionAndSendingEntriesService` снимает snapshot EntityEntry (Added/Modified/Deleted) в момент SavingChanges. Генерирует типы: "ObjectCreated", "ObjectChanged", "ObjectDeleted". PropertyValues через ToString().

Без explicit transaction: `AuditSaveChangesInterceptor` отправляет в `SavedChanges`/`SaveChangesFailed`.
С explicit transaction: `AuditTransactionInterceptor` отправляет в `TransactionCommitted`.

```csharp
services.AddAuditInterceptors<InterceptionAndSendingEntriesService>();
options.AddInterceptors(sp.GetRequiredService<IAuditSaveChangesInterceptor>(), sp.GetRequiredService<IAuditDbTransactionInterceptor>());
```

## Logger (Dex.Audit.Logger)

`Channel<AuditEventBaseInfo>` (bounded, int.MaxValue). `AuditLogger` пишет при `EventId.Id == 10_000_000` (`AuditLoggerConstants.AuditEventId`). `AuditLoggerReader` (BackgroundService) читает с `ReadEventsInterval` и вызывает IAuditWriter. Extension: `logger.LogAudit()`, `logger.LogAuditInfo()`, `LogAuditWarning()`, `LogAuditError()`, `LogAuditCritical()`.

## MediatR (Dex.Audit.MediatR)

`AuditBehavior<TRequest, TResponse>` (IPipelineBehavior), TRequest : `AuditRequest<TResponse>`, TResponse : `IAuditResponse`. Аудитирует success/failure, re-throws. `AuditRequest<T>`: abstract properties EventType, EventObject, Message.

## DI-регистрация

```csharp
// Клиент (Simple-реализация с MassTransit)
services.AddSimpleAuditClient(config);                          // BaseConfigurator + SimpleCache + SimpleSettings
services.AddSimpleAuditClient<TConfigurator>(config);           // кастомный конфигуратор
// MassTransit endpoints:
busContext.AddAuditClientSendEndpoint();                        // отправка событий
busConfigurator.AddSimpleAuditClientConsumer();                 // приём обновлений настроек
busContext.AddSimpleAuditClientReceiveEndpoint(configurator);

// Клиент (gRPC)
services.AddGrpcAuditClient<TConfigurator, TCacheRepo>(config, httpHandler?);

// Сервер
services.AddSimpleAuditServer<TDbContext>(config);
busConfigurator.AddAuditServerConsumer();
configurator.AddAuditServerReceiveEndpoint(context);

// Interceptors, Logger, MediatR
services.AddAuditInterceptors<TInterceptionService>();
loggingBuilder.AddAuditLogger();
mediatrConfig.AddPipelineAuditBehavior();
```

## Конфигурация

`AuditEventOptions`: MinSeverityLevel, SystemName (required). `AuditCacheOptions`: RefreshInterval. `AuditGrpcOptions`: ServerAddress. `AuditLoggerOptions`: ReadEventsInterval.

## Ограничения и gotchas

- Event types ОБЯЗАТЕЛЬНО регистрировать на сервере (`IAuditServerSettingsService.AddOrUpdateSettingsAsync()`) до отправки. Незарегистрированные типы игнорируются
- DB-generated IDs (auto-increment, sequences) будут 0/null в аудит-сообщении: snapshot снимается ДО SaveChanges
- PropertyValues сериализуются через ToString(): для complex types реализовать кастомный `IInterceptionAndSendingEntriesService`
- `SimpleClientAuditSettingsService` использует `DangerousAcceptAnyServerCertificateValidator` (небезопасно для production)
- Rollback транзакции НЕ генерирует аудит-событие с isSuccess=false (нет обработчика TransactionRolledBack)
- Cache refresh race: до `RefreshInterval` (default 10s) задержка при MassTransit. gRPC streaming обновляет в реальном времени
- gRPC stream: `WaitHandle.WaitOne()` блокирует бесконечно на сервере; ungraceful shutdown оставляет стрим открытым
- Batch на сервере: до 1s задержка перед обработкой (TimeLimit). Высоконагруженные клиенты ощутят latency
- Logger: magic EventId = 10_000_000. Обычные log-вызовы без этого EventId НЕ создают аудит-события
- `IAuditEntity` должен быть на сущности ДО добавления в DbContext. Добавление интерфейса после tracking не даёт аудита
