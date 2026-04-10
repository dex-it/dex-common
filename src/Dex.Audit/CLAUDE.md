# Dex.Audit

Клиент-серверная система аудита. Транспорт: MassTransit (RabbitMQ) или gRPC.

## Клиент

Запись событий: `IAuditWriter.WriteAsync(AuditEventBaseInfo, token)`.
`AuditEventBaseInfo` содержит: EventType, EventObject, Message, IsSuccess.
Обогащение событий перед отправкой: реализовать `IAuditEventConfigurator`.
Настройки аудита (какие события аудитировать) запрашиваются через `IAuditSettingsService` и кэшируются в `IAuditSettingsCacheRepository`.

DI-регистрация:
```csharp
services.AddSimpleAuditClient();                          // базовая реализация
services.AddSimpleAuditClient<TConfigurator>();            // с кастомным конфигуратором
services.AddAuditClient<TConfigurator, TCacheRepo, TSettingsService>(config);  // полная настройка
// MassTransit endpoint:
x.RegisterBus((context, configurator) => context.AddAuditClientSendEndpoint());
```

## Сервер

Приём событий, хранение, управление настройками аудита.
Хранилища: `IAuditEventsRepository` (события), `IAuditSettingsRepository` (настройки).
Кэш настроек: `IAuditSettingsCacheRepository`, обновляется через `RefreshCacheWorker` (фоновый сервис).

DI-регистрация:
```csharp
services.AddSimpleAuditServer<TDbContext>();
services.AddAuditServer<TEventsRepo, TSettingsRepo, TCacheRepo, TSettingsService>(config);
// MassTransit:
x.AddAuditServerConsumer();
configurator.AddAuditServerReceiveEndpoint(context, useInMemoryOutbox: true);
```

## EF Core Interceptors (автоаудит)

Сущности для автоаудита должны реализовать маркерный интерфейс `IAuditEntity` (без методов).
Два interceptor-а: `IAuditSaveChangesInterceptor`, `IAuditDbTransactionInterceptor`.
Сервис `IInterceptionAndSendingEntriesService` отслеживает состояние EntityEntry (Added/Modified/Deleted)
и генерирует события "ObjectCreated", "ObjectChanged", "ObjectDeleted".

```csharp
services.AddAuditInterceptors<TInterceptionService>();
// В DbContext:
options.AddInterceptors(
    sp.GetRequiredService<IAuditSaveChangesInterceptor>(),
    sp.GetRequiredService<IAuditDbTransactionInterceptor>()
);
```

## Конфигурация (appsettings.json)

```json
{
  "AuditEventOptions": { "MinSeverityLevel": "Low", "SystemName": "MyService" },
  "AuditCacheOptions": { "RefreshInterval": "00:00:10" },
  "AuditGrpcOptions": { "ServerAddress": "http://localhost:7240" }
}
```

## Ограничения и gotchas

- Типы событий ОБЯЗАТЕЛЬНО регистрируются на сервере через `IAuditServerSettingsService.AddOrUpdateSettingsAsync()` до того, как клиент начнёт их отправлять. Незарегистрированные типы игнорируются.
- EF interceptors захватывают состояние в момент SaveChanges, не в момент изменения сущности
- Значения свойств сериализуются через ToString(): для сложных типов реализовать кастомный `IInterceptionAndSendingEntriesService`
- gRPC-реализация требует настройки HttpMessageHandler для валидации сертификатов
