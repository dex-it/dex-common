# Dex.Cap

Три паттерна транзакционной надёжности: Outbox (исходящие), Inbox (входящие) и OnceExecutor (идемпотентность). Общий код в `Dex.Cap.Common` (интерфейсы) и `Dex.Cap.Common.Ef` (транзакции, retry).

## Структура solution (18 проектов)

Common, Common.Ef, Outbox (+ Ef, Neo4j, AspNetScheduler), Inbox (+ Ef, AspNetScheduler), OnceExecutor (+ Ef, Neo4j, ClickHouse, Memory, AspNetScheduler), Outbox.OnceExecutor.MassTransit. Тесты: `Tests/Dex.Cap.Ef.Tests` (основной), `Tests/Dex.Cap.OnceExecutor.Memory.Test`, `Tests/Dex.Cap.ClickHouse.Test`, `Tests/Dex.Outbox.Command.Test`.

## Outbox

Сообщение реализует `IOutboxMessage`: `static abstract string OutboxTypeId` (дискриминатор, НЕ instance-свойство), `static virtual bool AllowAutoPublishing => true`, `static virtual bool DeleteImmediately => false`.

Постановка: `IOutboxService.EnqueueAsync<T>()` (correlationId, scheduledStartDate, lockTimeout). Сообщение добавляется в DbContext, но НЕ сохраняется: вызывающий код делает `SaveChangesAsync()` атомарно с бизнес-операцией.

Pipeline: Enqueue (в транзакции) -> Fetch (CTE + `FOR UPDATE SKIP LOCKED`, PostgreSQL-only) -> Process (`SemaphoreSlim(ConcurrencyLimit)`, per-message timeout) -> Complete (RepeatableRead, verify lock ownership).

DB-модель `OutboxEnvelope`: Id, CorrelationId, MessageType (discriminator), Content (JSON, System.Text.Json), Status (New=0/Failed=1/Succeeded=2), Retries, LockId + LockTimeout + LockExpirationTimeUtc (pessimistic lock), StartAtUtc, ActivityId (tracing). Индексы: `(ScheduledStartIndexing, Status, Retries)` с фильтром Status in (0,1); `CreatedUtc`; `CorrelationId`.

Обработчики: `IOutboxMessageHandler<T>`. Если `AllowAutoPublishing=true` и нет явного handler, `AddOutboxPublisher()` авто-публикует через `IPublishEndpoint`. Если `AllowAutoPublishing=false` и нет handler, job fails.

## Inbox

Сообщение реализует `IInboxMessage`: `static abstract string InboxTypeId` (дискриминатор). Обработчик `IInboxMessageHandler<T>` обязателен: выбираются только сообщения с зарегистрированным обработчиком.

Приём: `IInboxService.EnqueueAsync(message, new InboxMessageIdentity(messageId, consumerId), lockTimeout)`. В отличие от Outbox сохраняет НЕМЕДЛЕННО, своей транзакцией: смысл в фиксации сообщения до подтверждения источнику. Поэтому приём внутри чужой транзакции (открытая транзакция DbContext или окружающий `TransactionScope`) отвергается с `InboxException`: на откате сообщение исчезло бы после ack источнику. Возвращает `Accepted`/`Duplicate`; дубль — штатный исход, не исключение.

Pipeline: Enqueue (`INSERT ... ON CONFLICT ("MessageId","ConsumerId") DO NOTHING`) -> Claim (CTE + `FOR UPDATE SKIP LOCKED` + аренда, PostgreSQL-only) -> Process (обработчик + перевод в Succeeded ОДНОЙ транзакцией) -> Fail (отдельной транзакцией после отката).

DB-модель `InboxEnvelope` (таблица `cap.inbox`): Id, MessageId + ConsumerId (уникальный индекс = ключ дедупликации), MessageType, Content (JSON), ActivityId, Retries, Status (New=0/Failed=1/Succeeded=2/DeadLettered=3), ErrorMessage + Error, CreatedUtc, Updated, StartAtUtc, ScheduledStartIndexing, LockTimeout + LockId + LockExpirationTimeUtc. Индексы: уникальный `(MessageId, ConsumerId)`; частичный `(ScheduledStartIndexing, Status)` с фильтром `ScheduledStartIndexing IS NOT NULL`; `(Status, CreatedUtc)` под чистку.

Транспорт-агностичен: ключ дедупликации задаёт вызывающая сторона, поэтому источником может быть и шина (`context.MessageId`), и HTTP (`Idempotency-Key`). Зависимости на MassTransit нет.

## OnceExecutor

Два варианта: `IOnceExecutor<TOptions>` (прямая работа с DbContext) и `IStrategyOnceExecutor<TArg, TResult>` (стратегия с `IsAlreadyExecuted`, `Execute`, `Read`). DB-модель `LastTransaction`: PK `string IdempotentKey`, `DateTime Created`.

Отличие от Inbox: OnceExecutor выполняет логику ИНЛАЙН в консьюмере и не хранит тело сообщения, поэтому у него нет ни отложенной обработки, ни ретраев с состоянием, ни статусов. Это дедупликатор эффекта, а не инбокс.

## DI-регистрация

```csharp
services.AddOutbox<TDbContext>();
services.AddDefaultOutboxScheduler<TDbContext>(periodSeconds: 5, cleanupDays: 7);
services.AddOutboxPublisher();  // авто-обработчик для AllowAutoPublishing=true
services.AddInbox<TDbContext>();
services.AddDefaultInboxScheduler<TDbContext>(periodSeconds: 30, cleanupDays: 30);
services.AddOnceExecutor<TDbContext>();
services.AddStrategyOnceExecutor<TArg, TResult, TStrategy, TDbContext>();
services.AddDefaultOnceExecutorScheduler<TDbContext>(periodSeconds, cleanupDays);
```

EF-конфигурация (обязательно в `OnModelCreating`):
```csharp
modelBuilder.OutboxModelCreating();       // таблица OutboxEnvelope
modelBuilder.InboxModelCreating();        // таблица InboxEnvelope
modelBuilder.OnceExecutorModelCreating(); // таблица LastTransaction
```

## Конфигурация

`OutboxOptions`: Retries=3, MessagesToProcess=100, ConcurrencyLimit=1, GetFreeMessagesTimeout=20s.
`OutboxHandlerOptions`: Period=30s, CleanupInterval=1h, CleanupOlderThan=30d.
`EfTransactionOptions`: TransactionScopeOption, IsolationLevel (ReadCommitted), TimeoutInSeconds=60, ClearChangeTrackerOnRetry=true. Пресеты: `DefaultRepeatableRead`, `DefaultRequiresNew`, `DefaultSuppress`.

## MassTransit-интеграция

`IdempotentConsumer<TMessage, TDbContext>`: base class для exactly-once обработки. Использует `context.MessageId` как idempotent key. По умолчанию `EfTransactionOptions.DefaultRequiresNew`. Переопределяемые: `TransactionOptions`, `GetIdempotentKey()`.

## Провайдеры

Outbox: EF Core (PostgreSQL-only SQL), Neo4j (Cypher, без pessimistic locks). OnceExecutor: EF Core, ClickHouse (TinyLog, без транзакций), Neo4j, Memory (`IDistributedCache`, для тестов).

## Тесты

NUnit. `BaseTest`: уникальная БД на тест (`"db_test_" + DateTime.Now.Ticks`), `EnsureDeleted/Created` в Setup/TearDown. `TestDbContext`: PostgreSQL, `EnableRetryOnFailure(3)`, вызывает `OutboxModelCreating()` + `OnceExecutorModelCreating()`.

## Ограничения и gotchas

- `OutboxTypeId` это `static abstract`, не instance-свойство. Реализация: `public static string OutboxTypeId => "guid-here"`
- xmin concurrency: исключить `OutboxEnvelope` и `LastTransaction` из `UseXminAsConcurrencyToken` (конфликт с LockId)
- LockTimeout (default 30s, min 10s) ОБЯЗАН превышать время обработки сообщения, иначе дубли
- `UnsavedChangesDetectedException`: если ChangeTracker содержит несохранённые изменения после modificator в OnceExecutor
- `ClearChangeTrackerOnRetry=true` (default): на retry ВСЕ tracked entities очищаются
- Дискриминатор ищется через `AppDomain.CurrentDomain.GetAssemblies()` (reflection). Сборка с IOutboxMessage должна быть загружена
- Background service стартует с random 5-15s delay (split brain prevention). В тестах не ждать мгновенной обработки
- `AddHealthChecks()` нужно вызвать ДО `AddDefaultOutboxScheduler()`, иначе health check не регистрируется
- Outbox.Ef работает ТОЛЬКО с PostgreSQL (CTE + FOR UPDATE SKIP LOCKED)
- `DiscriminatorResolveException` при enqueue, если тип сообщения не найден в загруженных сборках
- Inbox.Ef тоже ТОЛЬКО PostgreSQL (`FOR UPDATE SKIP LOCKED` + `ON CONFLICT DO NOTHING`)
- Inbox: `CleanupOlderThan` это НЕ просто ретеншен, а окно дедупликации. Удалили строку — повтор того же сообщения будет принят как новый. Держать выше максимального горизонта передоставки источника
- Inbox: `ConsumerId` обязан быть стабилен между рестартами и одинаков на всех инстансах. Значение, меняющееся от инстанса к инстансу, отключает дедупликацию
- Inbox: обработчик НЕ должен вызывать `SaveChangesAsync` сам — коммит делает транзакция обработки, вместе с переводом сообщения в Succeeded
- Inbox: транзакция покрывает только БД. Внешние вызовы обработчика (HTTP, пуши, брокер) повторятся при ретрае, поэтому обязаны быть идемпотентны сами по себе. Гарантия — effectively once, не exactly once
- Inbox: коллизия дискриминаторов роняет построение реестра (`DiscriminatorConflictException`), в отличие от Outbox, который молча берёт первый тип
