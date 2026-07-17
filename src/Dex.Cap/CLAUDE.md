# Dex.Cap

Три паттерна транзакционной надёжности: Outbox (исходящие), Inbox (входящие) и OnceExecutor (идемпотентность). Общий код в `Dex.Cap.Common` (интерфейсы) и `Dex.Cap.Common.Ef` (транзакции, retry).

## Структура solution (22 проекта: 16 библиотек + 6 тестовых)

Common, Common.Ef, Outbox (+ Ef, Neo4j, AspNetScheduler), Inbox (+ Ef, AspNetScheduler), OnceExecutor (+ Ef, Neo4j, ClickHouse, Memory, AspNetScheduler), Outbox.OnceExecutor.MassTransit. Тесты: `Tests/Dex.Cap.Ef.Tests` (основной, PostgreSQL), `Tests/Dex.Cap.OnceExecutor.Memory.Test`, `Tests/Dex.Cap.ClickHouse.Test`, `Tests/Dex.Outbox.Command.Test`, `Dex.Cap.AspNet.Test`.

## Outbox

Сообщение реализует `IOutboxMessage`: `static abstract string OutboxTypeId` (дискриминатор, НЕ instance-свойство), `static virtual bool AllowAutoPublishing => true`, `static virtual bool DeleteImmediately => false`.

Постановка: `IOutboxService.EnqueueAsync<T>()` (correlationId, scheduledStartDate, lockTimeout). Сообщение добавляется в DbContext, но НЕ сохраняется: вызывающий код делает `SaveChangesAsync()` атомарно с бизнес-операцией.

Pipeline: Enqueue (в транзакции) -> Fetch (CTE + `FOR UPDATE SKIP LOCKED`, PostgreSQL-only) -> Process (`SemaphoreSlim(ConcurrencyLimit)`, per-message timeout) -> Complete (RepeatableRead, verify lock ownership).

DB-модель `OutboxEnvelope`: Id, CorrelationId, MessageType (discriminator), Content (JSON, System.Text.Json), Status (New=0/Failed=1/Succeeded=2), Retries, LockId + LockTimeout + LockExpirationTimeUtc (pessimistic lock), StartAtUtc, ActivityId (tracing). Индексы: `(ScheduledStartIndexing, Status, Retries)` с фильтром Status in (0,1); `CreatedUtc`; `CorrelationId`.

Обработчики: `IOutboxMessageHandler<T>`. Если `AllowAutoPublishing=true` и нет явного handler, `AddOutboxPublisher()` авто-публикует через `IPublishEndpoint`. Если `AllowAutoPublishing=false` и нет handler, job fails.

## Inbox

Сообщение реализует `IInboxMessage`: `static abstract string InboxTypeId` (дискриминатор). Обработчик `IInboxMessageHandler<T>` обязателен: выбираются только сообщения с зарегистрированным обработчиком.

Приём: `IInboxService.EnqueueAsync(message, new InboxMessageIdentity(messageId, consumerId), lockTimeout)`. В отличие от Outbox сохраняет НЕМЕДЛЕННО, своей транзакцией: смысл в фиксации сообщения до подтверждения источнику. Поэтому приём внутри чужой транзакции (открытая транзакция DbContext или окружающий `TransactionScope`) отвергается с `InboxException`: на откате сообщение исчезло бы после ack источнику. Возвращает `Accepted`/`Duplicate`; повтор — штатный исход, не исключение.

Pipeline: Enqueue (`INSERT ... ON CONFLICT ("MessageId","ConsumerId") DO NOTHING`) -> Claim (CTE + `FOR UPDATE SKIP LOCKED` + аренда одним стейтментом, PostgreSQL-only) -> Process (обработчик + перевод в Succeeded ОДНОЙ транзакцией) -> Fail (отдельной транзакцией после отката) -> Cleanup.

DB-модель `InboxEnvelope` (таблица `cap.inbox`): Id, MessageId + ConsumerId (уникальный индекс = ключ дедупликации), MessageType, Content (JSON), ActivityId, Retries, Status (New=0/Failed=1/Succeeded=2/DeadLettered=3), ErrorMessage + Error, CreatedUtc, Updated, StartAtUtc, ScheduledStartIndexing, LockTimeout + LockId + LockExpirationTimeUtc.

Индексы: уникальный `(MessageId, ConsumerId)`; частичный `(ScheduledStartIndexing, Status)` с фильтром `ScheduledStartIndexing IS NOT NULL` (завершённые выходят из индекса, он не растёт вместе с историей); `(Status, MessageType, CreatedUtc)` под чистку — MessageType стоит ДО CreatedUtc, потому что чистка обязана быстро отвечать «своих строк нет».

Аренда: захват проставляет `LockId` и `LockExpirationTimeUtc = now + LockTimeout`. Обработка гасится на 5 секунд раньше окончания аренды, чтобы успеть зафиксировать исход. Фиксация идёт с предикатом владения (`LockId` + непросроченность), поэтому потерянная аренда не перезаписывает чужую работу. На пути успеха потеря аренды бросает `InboxLeaseLostException` (internal) и откатывает транзакцию обработчика: иначе эффект применился бы дважды. На пути неудачи это просто LogWarning.

Чистка: удаляет только `Succeeded` и только СВОИХ дискриминаторов (одну таблицу могут обслуживать несколько сервисов с разным ретеншеном). `DeadLettered` не удаляется никогда: это материал для ручного разбора. Удаление идёт пачками по 1000 по ctid с `FOR UPDATE SKIP LOCKED`, без `ORDER BY`, выход по ПУСТОЙ пачке. Если у сервиса нет обработчиков, чистка не смотрит ни на одну строку и пишет Warning.

Возврат `DeadLettered` в обработку идёт через публичный `IInboxDeadLetterService` (`RequeueAsync` по паре MessageId+ConsumerId, `RequeueAllAsync` массово), а не правкой таблицы руками: согласованный сброс Status/Retries/StartAtUtc/ScheduledStartIndexing и снятие аренды одним EF `ExecuteUpdate` (без сырого SQL, поэтому переносится на любой EF-провайдер), только свои дискриминаторы и только строки в статусе `DeadLettered`, поэтому вызов идемпотентен.

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
modelBuilder.InboxModelCreating();        // таблица cap.inbox
modelBuilder.OnceExecutorModelCreating(); // таблица LastTransaction
```

`AddInbox` даёт приём, обработку и warm-up реестра типов (hosted service: коллизия дискриминаторов роняет старт). Фоновые обработчик с чистильщиком и `IInboxCleanupDataProvider` регистрирует ТОЛЬКО `AddDefaultInboxScheduler`/`AddInboxScheduler`. Синглтоны инбокса идут через `TryAdd` (побеждает регистрация ДО `AddInbox`), scoped-сервисы, включая `IInboxSerializer`, — обычным `Add` (побеждает регистрация ПОСЛЕ).

## Конфигурация

`OutboxOptions`: Retries=3, MessagesToProcess=100, ConcurrencyLimit=1, GetFreeMessagesTimeout=20s.
`OutboxHandlerOptions`: Period=30s, CleanupInterval=1h, CleanupOlderThan=30d.
`InboxOptions`: Retries=3, MessagesToProcess=25, ConcurrencyLimit=1, GetFreeMessagesTimeout=20s (минимум 1s: таймаут команды задаётся целыми секундами, доля усекается в ноль, а ноль означает «не задан»). Валидируются на старте хоста (`ValidateOnStart`): неверная конфигурация роняет запуск.
`InboxHandlerOptions`: Period=30s, CleanupInterval=1h, CleanupOlderThan=30d, HandlerInitDelay=5-15s, CleanerInitDelay=20-40s.
`EfTransactionOptions`: TransactionScopeOption, IsolationLevel (ReadCommitted), TimeoutInSeconds=60, ClearChangeTrackerOnRetry=true. Пресеты: `DefaultRepeatableRead`, `DefaultRequiresNew`, `DefaultSuppress`.

Ретрай-стратегии инбокса: `DefaultInboxRetryStrategy` (без задержки), `IncrementalInboxRetryStrategy(interval)`, `ExponentialInboxRetryStrategy(baseDelay, maxDelay)` — задержка `baseDelay * 2^(N-1)`, ограничена `maxDelay`, укорочена джиттером до 10%. Задержка отсчитывается от момента ОТКАЗА, а не от прежнего `StartAtUtc`: иначе на отстающей обработке все попытки сгорают мгновенно. Джиттер только укорачивает, поэтому `maxDelay` остаётся жёстким потолком, а разброс сохраняется и на потолке; без джиттера сообщения, отказавшие одновременно, становились бы готовыми одновременно.

## MassTransit-интеграция

`IdempotentConsumer<TMessage, TDbContext>`: base class для exactly-once обработки. Использует `context.MessageId` как idempotent key. По умолчанию `EfTransactionOptions.DefaultRequiresNew`. Переопределяемые: `TransactionOptions`, `GetIdempotentKey()`.

## Провайдеры

Outbox: EF Core (PostgreSQL-only SQL), Neo4j (Cypher, без pessimistic locks). Inbox: EF Core (PostgreSQL-only). OnceExecutor: EF Core, ClickHouse (TinyLog, без транзакций), Neo4j, Memory (`IDistributedCache`, для тестов).

## Метрики и health check

Inbox, `Meter("Inbox")`: счётчики `ProcessCount`, `EmptyProcessCount`, `ProcessJobCount`, `ProcessJobSuccessCount`, `ProcessJobFailedCount`, `DeadLetteredCount`, `DuplicateCount`, `ExpiredBeforeStartCount`, `LeaseLostCount`; гистограмма `ProcessDuration`; ObservableUpDownCounter `FreeJobCount` (глубина очереди) и `DeadLetteredJobCount`. Обе наблюдаемые величины считают только СВОИ дискриминаторы, иначе алерт залипал бы на чужих сообщениях. UpDownCounter, а не Counter: асинхронный Counter обязан быть монотонным, и экспортёр прочитал бы падение как сброс.

`ProcessJobSuccessCount`, `ProcessJobFailedCount` и `DeadLetteredCount` считают ФАКТИЧЕСКУЮ запись, а не намерение: при потерянной аренде исход не пишется, и счётчик захоронений иначе указывал бы на строку, которой в `DeadLettered` нет. У успеха тот же принцип диктует другое МЕСТО: неудача пишется своей транзакцией, поэтому её считает провайдер данных, а успех пишется в транзакцию обработчика и на выходе из `JobSucceed` ещё не закоммичен (откатить его может и сам коммит, и переигровка блока стратегией повторов EF). Факт известен только владельцу транзакции, поэтому успех считает `InboxJobHandlerEf.ProcessJobCore` после её закрытия.

`ProcessCount` считает состоявшиеся циклы (выборка дошла до хранилища), а не задачи, и он же ставит признак жизни для health check. Отметка не ставится ни до выборки (иначе недоступность БД выглядела бы как здоровье, ведь заходы продолжались бы с той же частотой), ни только по задачам (иначе партия целиком из непригодных строк не дала бы признака жизни при живом обработчике).

Устойчиво ненулевые `ExpiredBeforeStartCount` и `LeaseLostCount` означают одно и то же: `LockTimeout` мал для `MessagesToProcess`, потому что аренда всей партии тикает с момента захвата. Первый счётчик ловит случай, когда аренда умерла в очереди на обработку, второй - когда во время самой обработки. Ни один из них не тратит попытку: истечение аренды это ошибка размера `LockTimeout`, а не отказ сообщения. Иначе наказывался бы ИЗБИРАТЕЛЬНО обработчик, уважающий токен отмены (он отдаёт управление, пока аренда ещё жива, и исход записывается), а игнорирующий токен уходил бы безнаказанным.

Бюджет времени на сообщение: `(LockTimeout - 5s) * ConcurrencyLimit / MessagesToProcess`. На дефолтах секунда.

Health check инбокса регистрируется автоматически планировщиком под тегом `inbox-scheduler`, отдаёт `Degraded`, если обработчик не отчитывался дольше `Period * 2`.

## Тесты

NUnit. `BaseTest`: уникальная БД на тест (`"db_test_" + DateTime.Now.Ticks`), `EnsureDeleted/Created` в Setup/TearDown. `TestDbContext`: PostgreSQL, `EnableRetryOnFailure(3)`, вызывает `OutboxModelCreating()` + `InboxModelCreating()` + `OnceExecutorModelCreating()`. Тесты идут через `EnsureCreated`, миграции в `Tests/.../Migrations` не применяются.

Полный набор (195 тестов) требует трёх контейнеров; CI поднимает их сам:
```bash
docker run -d --name pg -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD='...' -p 5432:5432 postgres:16
docker run -d --name rabbit -p 5672:5672 rabbitmq:3-management     # OnceExecutorRabbitTest1
docker run -d --name ch -p 9000:9000 clickhouse/clickhouse-server:24-alpine  # ClickHouse-тесты
dotnet test src/Dex.Cap/Dex.Cap.sln
```
Строка подключения: `Tests/Dex.Cap.Ef.Tests/appsettings.json`. Тестам инбокса нужен только PostgreSQL.

`GlobalUsings.cs` переопределяет `Assert` на `NUnit.Framework.Legacy.ClassicAssert`, у которого нет `Throws*`. Поэтому в тестах пишут полное имя с явным приведением лямбды: `NUnit.Framework.Assert.ThrowsAsync<T>((Func<Task>)(async () => ...))`.

## Ограничения и gotchas

- `OutboxTypeId`/`InboxTypeId` это `static abstract`, не instance-свойство. Реализация: `public static string InboxTypeId => "guid-here"`
- xmin concurrency: исключить `OutboxEnvelope`, `InboxEnvelope` и `LastTransaction` из `UseXminAsConcurrencyToken` (конфликт с LockId)
- LockTimeout (default 30s, диапазон 10s..1d) ОБЯЗАН превышать время обработки ВСЕЙ захваченной партии, а не одного сообщения: аренда всех сообщений тикает с момента захвата. Верхняя граница техническая: таймер отмены не принимает интервал длиннее ~24.8 суток
- `UnsavedChangesDetectedException`: если ChangeTracker содержит несохранённые изменения после modificator в OnceExecutor
- `ClearChangeTrackerOnRetry=true` (default): на retry ВСЕ tracked entities очищаются
- Дискриминатор ищется через `AppDomain.CurrentDomain.GetAssemblies()` (reflection). Сборка с сообщением должна быть загружена
- Background service стартует с random 5-15s delay (split brain prevention). В тестах и в ручных прогонах не ждать мгновенной обработки
- `AddHealthChecks()` нужно вызвать ДО `AddDefaultOutboxScheduler()`, иначе health check не регистрируется
- Inbox: порядок вызова `AddHealthChecks()` не важен, `AddInboxScheduler` регистрирует его сам. Но health check отдаёт `Degraded`, а ASP.NET Core по умолчанию мапит `Degraded` в HTTP 200: k8s посчитает вставший инбокс здоровым, если явно не задать `ResultStatusCodes`
- Outbox.Ef и Inbox.Ef работают ТОЛЬКО с PostgreSQL (CTE + `FOR UPDATE SKIP LOCKED`, `ON CONFLICT DO NOTHING`)
- `DiscriminatorResolveException` при enqueue, если тип сообщения не найден в загруженных сборках
- Inbox: `CleanupOlderThan` это НЕ просто ретеншен, а окно дедупликации. Удалили строку — повтор того же сообщения будет принят как новый. Держать выше максимального горизонта передоставки источника
- Inbox: `ConsumerId` обязан быть стабилен между рестартами и одинаков на всех инстансах. Значение, меняющееся от инстанса к инстансу, отключает дедупликацию
- Inbox: обработчик НЕ должен вызывать `SaveChangesAsync` сам — коммит делает транзакция обработки, вместе с переводом сообщения в Succeeded
- Inbox: тело сообщения лежит в БД и читается ПОСЛЕ деплоя, поэтому схема сообщения это контракт. Дефолтный `DefaultInboxSerializer` форсит `JsonStringEnumConverter`: enum пишутся ИМЕНАМИ, поэтому перестановка членов безопасна (числовой дефолт STJ молча переназначил бы смысл сохранённых тел). Ломающими остаются переименование члена enum, переименование поля или смена его типа (тело нечитаемо: ретраи, затем DeadLettered); добавление поля безопасно. Дефолт выставлен ДО релиза (сохранённых тел нет, цена нулевая); после релиза он сам становится контрактом. Меняется через свой `IInboxSerializer`
- Inbox: транзакция покрывает только БД. Внешние вызовы обработчика (HTTP, пуши, брокер) повторятся при ретрае, поэтому обязаны быть идемпотентны сами по себе. Гарантия — effectively once, не exactly once
- Inbox: коллизия дискриминаторов роняет СТАРТ хоста (`DiscriminatorConflictException` из warm-up hosted service, регистрируется в `AddInbox`), в отличие от Outbox, который молча берёт первый тип. Пустой дискриминатор ловится там же (`DiscriminatorResolveException`)
- Inbox: набор символов дискриминатора НЕ ограничен — он уходит в SQL захвата параметром, а не подстановкой в текст. `urn:message:...` (MessageUrn MassTransit), `Outer+Inner`, кавычка и фигурная скобка допустимы. Сменить дискриминатор существующему типу нельзя: он лежит в БД
- Inbox: дискавери типов вынесен в `IInboxMessageTypeSource` (дефолт сканирует AppDomain), поэтому построение реестра тестируется без загрузки конфликтующих типов в процесс
- Inbox: строка с `LockTimeout` ниже минимума (правка руками, чужой писатель, изменённый дефолт колонки) уходит в `DeadLettered` с явной причиной. Иначе одна такая строка роняла бы материализацию всей партии и инбокс вставал бы навсегда
- Inbox: `InitDelayRange` в `Inbox.AspNetScheduler` — копия одноимённого типа из `Outbox.AspNetScheduler`. Пакеты намеренно не связаны, менять надо обе копии
