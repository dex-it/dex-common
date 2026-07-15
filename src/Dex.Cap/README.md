# Dex.Cap

A set of building blocks for reliable database-driven messaging and idempotent processing:

| Package | Purpose |
|---|---|
| `Dex.Cap.Common` / `Dex.Cap.Common.Ef` | Shared abstractions (`IOutboxMessage`, `IIdempotentKey`, `ITransactionOptions`) and EF Core transaction helpers (`ExecuteInTransactionAsync`). |
| `Dex.Cap.Outbox` | Transactional Outbox pattern. |
| `Dex.Cap.Outbox.Ef` | EF Core data provider for the outbox. |
| `Dex.Cap.Outbox.AspNetScheduler` | Hosted services that drain and clean up the outbox queue. |
| `Dex.Cap.Outbox.OnceExecutor.MassTransit` | MassTransit integration: auto-publishing handler and idempotent consumers. |
| `Dex.Cap.Inbox` | Transactional Inbox pattern: deduplicate incoming messages and process them in the background. |
| `Dex.Cap.Inbox.Ef` | EF Core data provider for the inbox. |
| `Dex.Cap.Inbox.AspNetScheduler` | Hosted services that drain and clean up the inbox queue. |
| `Dex.Cap.OnceExecutor` / `Dex.Cap.OnceExecutor.Ef` | Idempotent (once-only) execution by idempotency key. |
| `Dex.Cap.OnceExecutor.AspNetScheduler` | Hosted service that cleans up obsolete idempotency records. |

**Outbox, Inbox or OnceExecutor?** Outbox solves the *outgoing* side: publish a message atomically with the
database change that caused it. Inbox and OnceExecutor both solve the *incoming* side, but differently:

* `Dex.Cap.OnceExecutor` runs your logic **inline, inside the consumer**, and remembers the idempotency key so a
  redelivery short-circuits. The message body is never stored. Use it when the handler is fast and you are happy to
  hold the message on the broker while it runs.
* `Dex.Cap.Inbox` **stores the message, lets you acknowledge the source immediately, and processes it later** in a
  background worker with its own retries, dead-lettering and observability. Use it when processing is slow, when the
  source must not wait (an HTTP request), or when you want the retry policy to be yours rather than the broker's.
  Deduplication is built in and is not optional.

---

# Dex.Cap.Outbox

Implementation of the Outbox pattern, which allows you to atomically insert outgoing commands for asynchronous execution into the database together with the main operation. The template guarantees that commands will not be lost.

* Performs the operation and stores messages in the outbox queue inside the same transaction.
* All messages of a logical operation are linked by a single `CorrelationId`.
* Avoids closures — state is passed explicitly.
* Allows registering services required by the operation.
* Verifies the success of the operation (verifySucceeded callback).
* Discriminates messages by type via `OutboxTypeId` for serialization and dispatching.

### Registration

```csharp
// Define an outbox message
public class OrderCreatedOutboxMessage : IOutboxMessage
{
    public static string OutboxTypeId => "3961131e-3961-4c38-8a30-09b91cb56d60";

    // Optional, default = true.
    // If false — the message will NOT be auto-published by PublisherOutboxHandler,
    // an explicit IOutboxMessageHandler<T> must be registered.
    public static bool AllowAutoPublishing => true;

    // Optional, default = false.
    // If true — the row is deleted from the DB immediately after the handler completes
    // (recommended for large or very frequent messages to keep the table small).
    public static bool DeleteImmediately => false;

    public string Args { get; init; } = "";
}

// Optional: implement a dedicated handler. If none is registered and AllowAutoPublishing == true,
// PublisherOutboxHandler<T> (from Dex.Cap.Outbox.OnceExecutor.MassTransit) will publish the message.
public class OrderCreatedOutboxMessageHandler(ILogger<OrderCreatedOutboxMessageHandler> logger)
    : IOutboxMessageHandler<OrderCreatedOutboxMessage>
{
    public Task Process(OrderCreatedOutboxMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processed at {Now}, Args: {Args}", DateTime.UtcNow, message.Args);
        return Task.CompletedTask;
    }
}
```

```csharp
// ConfigureServices
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions; // for AddOutboxPublisher()

services.AddOutbox<AppDbContext>();                         // core services + EF data provider
services.AddDefaultOutboxScheduler<AppDbContext>(           // background processing + cleanup
    periodSeconds: 30,                                       // poll interval
    cleanupDays: 30);                                        // delete processed messages older than N days

services.AddScoped<IOutboxMessageHandler<OrderCreatedOutboxMessage>,
                   OrderCreatedOutboxMessageHandler>();      // dedicated handler (optional)

services.AddOutboxPublisher();                              // auto-publish any IOutboxMessage
                                                            // with AllowAutoPublishing == true
```

```csharp
// OnModelCreating
modelBuilder.OutboxModelCreating();

// Remark: when using optimistic concurrency in PostgreSQL — exclude OutboxEnvelope:
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(OutboxEnvelope));
```

### Basic usage

```csharp
public class OrderService(IOutboxService outbox, AppDbContext db)
{
    public async Task PlaceOrderAsync(string name, CancellationToken ct)
    {
        await db.Users.AddAsync(new User { Name = name }, ct);
        await outbox.EnqueueAsync(new OrderCreatedOutboxMessage { Args = name }, cancellationToken: ct);

        await db.SaveChangesAsync(ct); // atomic: domain change + outbox row
    }
}
```

For automatic retries on transient DB errors and transactional guarantees, wrap the call in
`db.ExecuteInTransactionAsync(...)` from `Dex.Cap.Common.Ef` (see below).

### `IOutboxService.EnqueueAsync` parameters

| Parameter | Default | Notes |
|---|---|---|
| `correlationId` | `IOutboxService.CorrelationId` (auto) | Override to link multiple messages to an existing logical operation. |
| `startAtUtc` | `DateTime.UtcNow` | Schedule the message for later processing (no earlier than `now - 1h`). |
| `lockTimeout` | `30s` | Must be ≥ 10s and greater than the expected handler execution time, otherwise the message will be picked up again before the current handler finishes. |

### `IOutboxMessage` interface

Outbox messages must implement `Dex.Cap.Common.Interfaces.IOutboxMessage`.

| Member | Type | Default | Description |
|---|---|---|---|
| `OutboxTypeId` | `static abstract string` | — | Required, stable unique id of the message type (used as a discriminator for serialization and handler lookup). |
| `AllowAutoPublishing` | `static virtual bool` | `true` | Allow `PublisherOutboxHandler<T>` (via `AddOutboxPublisher()`) to handle this message. Set to `false` to force a dedicated handler. |
| `DeleteImmediately` | `static virtual bool` | `false` | If true, the envelope row is removed right after a successful handler run, instead of being kept until cleanup. Recommended for huge or high-frequency messages. |

### Outbox options

Configurable via `IOptions<OutboxOptions>` (defaults shown):

| Option | Default | Description |
|---|---|---|
| `Retries` | `3` | Number of retry attempts per message on transient errors. |
| `MessagesToProcess` | `100` | Batch size — how many messages are fetched and locked per cycle. Processing time of the whole batch counts from the moment of selection. |
| `ConcurrencyLimit` | `1` | Degree of parallel processing inside a batch. Recommended: `ConcurrencyLimit ≤ MessagesToProcess`. |
| `GetFreeMessagesTimeout` | `20s` | DB-side timeout for selecting free messages. |

### Retry strategy

By default a no-op strategy is used (retry at `UtcNow`). To space retries out:

```csharp
using Dex.Cap.Outbox.Extensions;

services.AddOutbox<AppDbContext>((_, configurator) =>
{
    configurator.UseOutboxIncrementalRetryStrategy(TimeSpan.FromSeconds(30));
});
```

Or provide a custom `IOutboxRetryStrategy`:

```csharp
services.AddOutbox<AppDbContext>((_, configurator) =>
{
    configurator.RetryStrategy = new MyExponentialBackoffStrategy();
});
```

### Type discriminator

`IOutboxTypeDiscriminatorProvider` scans the current `AppDomain` at first access and:

* builds the map of `OutboxTypeId → CLR type`;
* reports which discriminators the current process can handle (i.e. has a registered handler);
* collects discriminators marked with `DeleteImmediately == true`.

This is what enables a single outbox table to be safely shared between multiple services that consume different subsets of message types.

### Health check

`AddDefaultOutboxScheduler` registers a health check named `outbox-scheduler` (tagged `outbox-scheduler`) that surfaces background-service failures via ASP.NET HealthChecks.

---

# Dex.Cap.Outbox.OnceExecutor.MassTransit

Bridges the outbox to MassTransit.

### PublisherOutboxHandler

Open-generic handler that automatically publishes any `IOutboxMessage` (with `AllowAutoPublishing == true`) to MassTransit. Use it when the outbox row itself **is** the integration event.

```csharp
services.AddOutbox<AppDbContext>();
services.AddOutboxPublisher(); // registers IOutboxMessageHandler<> -> PublisherOutboxHandler<>
```

### IdempotentOutboxHandler / TransactionalOutboxHandler

Base classes for writing custom outbox handlers that need:

* **Idempotency** — guarantees a single execution per `IIdempotentKey.IdempotentKey` (or MassTransit `MessageId` if `IIdempotentKey` is not implemented); duplicates exit silently.
* **Transactionality** — wraps the handler in `ExecuteInTransactionAsync`.

### IdempotentConsumer / TransactionalConsumer (MassTransit consumers)

> Requires the RabbitMQ plugin `rabbitmq_delayed_message_exchange`.

Base consumers for messages coming directly from MassTransit (not via the outbox). Handle errors, write logs, support `Defer` (throw `DeferConsumerException` → republish into `delay_exchange` after the specified interval).

`IdempotentConsumer` adds idempotency on top of the base consumer.

---

# Dex.Cap.Inbox

Implementation of the Transactional Inbox pattern: an incoming message is stored first, acknowledged to its source
immediately, and processed later by a background worker inside a single database transaction.

* Deduplicates by `(MessageId, ConsumerId)` — a redelivery never produces a second row.
* Decouples the source from processing: the broker (or the HTTP caller) is released as soon as the row is committed.
* Processes the handler and the status change **in one transaction**: the business effect and "message handled"
  either both commit or both roll back.
* Retries with a configurable strategy, then dead-letters — a failing message never disappears silently and never
  loops forever.
* Scales horizontally: workers claim messages with `FOR UPDATE SKIP LOCKED` plus a lease, so concurrent instances
  never take the same message.
* Transport-agnostic: the core knows nothing about MassTransit, HTTP or gRPC.

### What it does not give you

The transaction covers your database only. **Any external call made by the handler (HTTP, broker, push service)
will be repeated if the message is retried**, so those calls must be idempotent on their own — via a provider-side
idempotency key or a lookup before the call. The inbox gives you *effectively once*, not *exactly once*.

### Registration

```csharp
// Define an inbox message
public class OrderCreatedInboxCommand : IInboxMessage
{
    // Discriminator. It is written to the database and read back after a restart or a deploy,
    // so it must never change for an existing type.
    public static string InboxTypeId => "3961131e-3961-4c38-8a30-09b91cb56d60";

    public string Args { get; init; } = "";
}

// A handler is mandatory: only messages with a registered handler are fetched by this service.
// It runs inside the processing transaction, so it must NOT commit by itself.
public class OrderCreatedInboxCommandHandler(AppDbContext db) : IInboxMessageHandler<OrderCreatedInboxCommand>
{
    public async Task Process(OrderCreatedInboxCommand message, CancellationToken cancellationToken)
    {
        await db.Orders.AddAsync(new Order { Args = message.Args }, cancellationToken);
        // no SaveChanges here — the inbox commits the effect together with the message status
    }
}
```

```csharp
// ConfigureServices
using Dex.Cap.Inbox.Ef.Extensions;

services.AddInbox<AppDbContext>();                        // core services + EF data provider
services.AddDefaultInboxScheduler<AppDbContext>(          // background processing + cleanup
    periodSeconds: 30,                                     // pause only when the queue is drained
    cleanupDays: 30);                                      // retention == deduplication window

services.AddScoped<IInboxMessageHandler<OrderCreatedInboxCommand>, OrderCreatedInboxCommandHandler>();
```

```csharp
// OnModelCreating
modelBuilder.InboxModelCreating();

// Remark: when using optimistic concurrency in PostgreSQL — exclude InboxEnvelope:
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(InboxEnvelope));
```

### Usage: message bus

```csharp
public class OrderCreatedConsumer(IInboxService inbox) : IConsumer<OrderCreatedEvent>
{
    // Stable per consumer: it is part of the deduplication key.
    private const string ConsumerId = nameof(OrderCreatedConsumer);

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var messageId = context.MessageId ?? throw new InvalidOperationException("MessageId is required for deduplication");

        // A duplicate is a normal outcome of at-least-once delivery, not an error:
        // returning normally acknowledges the message instead of sending it to the error queue.
        await inbox.EnqueueAsync(
            new OrderCreatedInboxCommand { Args = context.Message.Args },
            new InboxMessageIdentity(messageId.ToString("N"), ConsumerId),
            cancellationToken: context.CancellationToken);
    }
}
```

### Usage: HTTP

```csharp
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder(
    [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var status = await inbox.EnqueueAsync(
        new OrderCreatedInboxCommand { Args = request.Args },
        new InboxMessageIdentity(idempotencyKey, "POST /orders"),
        cancellationToken: cancellationToken);

    // Both outcomes are success for the caller: the message is accepted exactly once either way.
    return status == InboxEnqueueStatus.Accepted ? Accepted() : Ok();
}
```

### `IInboxService.EnqueueAsync` parameters

| Parameter | Meaning |
|---|---|
| `identity` | `(MessageId, ConsumerId)` — the deduplication key. `MessageId` must be stable across redeliveries; `ConsumerId` must be stable across restarts and identical on every instance. |
| `lockTimeout` | Lease duration, default 30s, minimum 10s. **Must exceed the time needed to drain the whole claimed batch**, not just one message: the lease of every message in a batch starts ticking at claim time. Otherwise the lease expires mid-flight, the outcome cannot be committed, and the message is handed to another worker. |

Unlike `IOutboxService.EnqueueAsync`, this method **persists immediately in its own transaction**: the point of the
inbox is to store the message *before* the source is acknowledged, and there is no business work to be atomic with.
For the same reason enqueuing inside a transaction of your own is rejected with an `InboxException`: an enclosing
`DbContext` transaction or an ambient `TransactionScope` would roll the message back while the source has already
been acknowledged.

### Inbox options

`InboxOptions` (validated on host start):

| Option | Default | Meaning |
|---|---|---|
| `Retries` | 3 | Attempts before the message is dead-lettered. |
| `MessagesToProcess` | 100 | Batch size claimed per cycle. |
| `ConcurrencyLimit` | 1 | Degree of parallelism; must not exceed `MessagesToProcess`. |
| `GetFreeMessagesTimeout` | 20s | Timeout of the claim query. |

`InboxHandlerOptions` (scheduler): `Period` 30s, `CleanupInterval` 1h, `CleanupOlderThan` 30d,
`HandlerInitDelay` 5–15s, `CleanerInitDelay` 20–40s.

`Period` is a pause **between drained cycles**, not a throughput limit: while the worker keeps claiming full
batches it continues without pausing.

### Message states

```
                    ┌──────────────────────────── retry (attempts left) ────────────────────────────┐
                    │                                                                               │
   Enqueue ──▶ [New] ──▶ claim (FOR UPDATE SKIP LOCKED + lease) ──▶ handler ──┬── success ──▶ [Succeeded] ──▶ cleanup after CleanupOlderThan
                    │                                                          │
                    └──────────────────────────────────────────────────────────┴── failure ──▶ [Failed] ──▶ … ──▶ [DeadLettered] (terminal, kept for review)
```

There is no separate "in progress" state on purpose: the in-flight marker is the lease
(`LockId` + `LockExpirationTimeUtc`). A crashed worker lets the lease expire and the message returns to the queue,
whereas a status flag would stay stuck forever.

### Retention is the deduplication window

`CleanupOlderThan` deletes processed messages, and the row *is* the deduplication key. Once it is gone, a
redelivery of that message is accepted as new. Keep the retention comfortably above the maximum redelivery horizon
of the source. `DeadLettered` messages are never deleted by cleanup — they exist to be investigated, and
`DeadLetteredJobCount` reports how many are waiting.

Returning a message to processing after you fixed the cause takes three columns, not one: the status alone is not
enough, because the fetch requires `ScheduledStartIndexing IS NOT NULL` and the next failure would bury the message
again on the spot unless `Retries` is below `InboxOptions.Retries`.

```sql
UPDATE cap.inbox
SET "Status" = 0, "Retries" = 0, "ScheduledStartIndexing" = "StartAtUtc"
WHERE "Id" = '...';
```

### Retry strategy

```csharp
services.AddInbox<AppDbContext>(
    options => options.Retries = 6,
    (_, configurator) =>
        configurator.UseExponentialStrategy(baseDelay: TimeSpan.FromMinutes(1), maxDelay: TimeSpan.FromMinutes(30)));
```

The delay is measured **from the moment the attempt failed**, so a backlog does not eat it.

Default is no extra delay — the next cycle picks the message up. `UseIncrementalStrategy(interval)` and
`UseExponentialStrategy(baseDelay, maxDelay)` are also available.

Two things to size correctly, or the strategy is decorative:

* A delay below `Period` (default 30s) is not observable: the next attempt happens on the next cycle anyway.
* The exponent is bounded by `Retries`. The attempt that exhausts the limit dead-letters the message without
  computing a delay, so with `Retries = N` the multiplier never exceeds `2^(N-2)`. With the default `Retries = 3`
  you only ever get `baseDelay` and `2*baseDelay`, and `maxDelay` is unreachable.

### Health check

`AddInboxScheduler` registers the `inbox-scheduler` health check (tag `inbox-scheduler`) itself, so the registration
order does not matter. It reports `Degraded` when the last processing cycle is older than `2 × Period`.

Note that ASP.NET Core maps `Degraded` to HTTP 200 by default, so a Kubernetes probe treats a stalled inbox as
healthy. If you want a stalled worker to fail the probe, map it explicitly:

```csharp
app.MapHealthChecks("/health/inbox", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("inbox-scheduler"),
    ResultStatusCodes = { [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable },
});
```

### Metrics

Meter `Inbox`: `ProcessCount`, `EmptyProcessCount`, `ProcessJobCount`, `ProcessJobSuccessCount`,
`ProcessJobFailedCount`, `DeadLetteredCount`, `DuplicateCount`, `ExpiredBeforeStartCount`, `ProcessDuration`,
plus two observable up/down counters: `FreeJobCount` (depth of what *this* service can handle) and
`DeadLetteredJobCount` (buried messages awaiting review, never removed by cleanup).

A steadily non-zero `ExpiredBeforeStartCount` means the lease dies while the claimed batch is still draining:
raise `lockTimeout`, lower `MessagesToProcess`, or raise `ConcurrencyLimit`.

### Limitations

* PostgreSQL only (`Dex.Cap.Inbox.Ef` uses `FOR UPDATE SKIP LOCKED` and `ON CONFLICT`).
* Message types are discovered by reflection over loaded assemblies, so the assembly declaring `IInboxMessage`
  implementations must be loaded. In practice registering the handler loads it. The registry is built during host
  start (`AddInbox` registers a warm-up hosted service), so a duplicate, empty or quote-containing discriminator
  fails the host start rather than surfacing later inside the background worker.
* Ordering between messages is not guaranteed; design handlers to be order-independent.

---

# Dex.Cap.OnceExecutor

Guarantees that an operation is performed exactly once for a given idempotency key. On a repeat call the modification is skipped and the previously produced value (if any) is returned.

### Registration

```csharp
using Dex.Cap.OnceExecutor.Ef.Extensions;

services.AddOnceExecutor<AppDbContext>();
services.AddDefaultOnceExecutorScheduler<AppDbContext>(   // background cleanup of LastTransaction rows
    periodSeconds: 30,
    cleanupDays: 30);
```

```csharp
// OnModelCreating
modelBuilder.OnceExecutorModelCreating();

// Remark: when using optimistic concurrency in PostgreSQL — exclude LastTransaction:
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(LastTransaction));
```

### Basic usage

```csharp
var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, AppDbContext>>();

var user = await executor.ExecuteAsync(
    idempotentKey: "create-user-42",
    modificator:  (db, ct) => db.Users.AddAsync(new User { Name = "Bob" }, ct).AsTask(),
    selector:     (db, ct) => db.Users.FirstOrDefaultAsync(x => x.Name == "Bob", ct));
```

Pass `IEfTransactionOptions` to control isolation level and command timeout:

```csharp
await executor.ExecuteAsync(
    "key",
    (db, ct) => db.Users.AddAsync(new User { Name = "Bob" }, ct).AsTask(),
    options: new EfTransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead });
```

### Strategy-based usage

Encapsulate idempotency, modification and read in a single class implementing `IOnceExecutionStrategy<TArg, IEfTransactionOptions, TResult>`:

```csharp
public class CreateUserStrategy(AppDbContext db)
    : IOnceExecutionStrategy<CreateUserRequest, IEfTransactionOptions, string>
{
    public IEfTransactionOptions? Options { get; set; }
        = new EfTransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead };

    public Task<bool> IsAlreadyExecutedAsync(CreateUserRequest arg, CancellationToken ct)
        => db.Users.AnyAsync(x => x.Name == arg.Name, ct);

    public async Task ExecuteAsync(CreateUserRequest arg, CancellationToken ct)
        => await db.Users.AddAsync(new User { Name = arg.Name, Years = 18 }, ct);

    public async Task<string?> ReadAsync(CreateUserRequest arg, CancellationToken ct)
        => (await db.Users.SingleOrDefaultAsync(x => x.Name == arg.Name, ct))?.Name;
}
```

```csharp
services.AddStrategyOnceExecutor<CreateUserRequest, string, CreateUserStrategy, AppDbContext>();

var executor = sp.GetRequiredService<IStrategyOnceExecutor<CreateUserRequest, string>>();
var name = await executor.ExecuteAsync(new CreateUserRequest { Name = "Bob" }, ct);
```

---

# Dex.Cap.Common.Ef — Transaction helpers

`Dex.Cap.Common.Ef` ships two `DbContext` extension methods that wrap the operation in EF Core's `IExecutionStrategy` (transient-error retries) plus a transaction:

| Method | Transaction kind | Status | When to use |
|---|---|---|---|
| `ExecuteInTransactionAsync` | `IDbContextTransaction` (explicit) | **Recommended** | Default choice. Required for CQRS / multi-database scenarios to avoid `Ambient transaction detected` errors and DTC escalation. Supports nested (reentrant) calls on the same `DbContext` instance. |
| `ExecuteInTransactionScopeAsync` | `System.Transactions.TransactionScope` (ambient) | `[Obsolete]` | Only for the rare case when atomicity across **different** `DbContext` instances is genuinely needed. |

### ExecuteInTransactionAsync

```csharp
using Dex.Cap.Common.Ef.Extensions;

await db.ExecuteInTransactionAsync(
    operation: async ct =>
    {
        await db.Users.AddAsync(new User { Name = "Bob" }, ct);
        await outbox.EnqueueAsync(new OrderCreatedOutboxMessage { Args = "Bob" }, cancellationToken: ct);
        // SaveChangesAsync is called automatically before commit, but you may call it inside the operation
    },
    verifySucceeded: ct => db.Users.AnyAsync(u => u.Name == "Bob", ct),
    options: new EfTransactionOptions
    {
        IsolationLevel    = IsolationLevel.ReadCommitted, // default
        TimeoutInSeconds  = 60,                           // default
        ClearChangeTrackerOnRetry = true,                 // default
    },
    cancellationToken: ct);
```

Behaviour:

* On a transient `NpgsqlException` or `TimeoutException` the whole block is retried by `IExecutionStrategy`.
* `verifySucceeded` is called by the strategy to decide whether a retry is actually needed after an ambiguous failure.
* Throws `UnsavedChangesDetectedException` if the `ChangeTracker` already contains changes when the (root) call starts.
* Nested calls on the **same** `DbContext` instance participate in the existing transaction; a stricter isolation level than the outer one is rejected with `InvalidOperationException`.
* Nested calls on a **different** `DbContext` instance throw `InvalidOperationException` (silent atomicity loss is not allowed).

---

# Breaking changes

## Transactions

| Version | PR | Change |
|---|---|---|
| **8.3** | [#219](https://github.com/dex-it/dex-common/pull/219) | New `DbContext.ExecuteInTransactionAsync` (`IDbContextTransaction`) is the recommended way to wrap outbox / once-executor calls. `ExecuteInTransactionScopeAsync` (`TransactionScope`) is marked `[Obsolete]` and is no longer recommended for async / CQRS code (ambient transactions can promote to DTC). |
| 8.x | [#188](https://github.com/dex-it/dex-common/pull/188) | `ITransactionOptions` moved and renamed; `TransactionalConsumer` and `TransactionalOutboxHandler` introduced. |

## Type discriminator

The discriminator (logical id of a message type, used both for serialization and for handler dispatching across services that share an outbox table) went through several iterations. Each iteration is a breaking change for consumers that used the previous shape.

| Version | PR / Commit | Change |
|---|---|---|
| **8.2.4** | [#214](https://github.com/dex-it/dex-common/pull/214) (`ccf063f`) | `IOutboxMessage.DeleteImmediately` added as `static virtual bool DeleteImmediately => false`. Opt-in: row is removed right after a successful handler instead of being kept until cleanup. `IOutboxTypeDiscriminatorProvider.ImmediatelyDeletableMessages` exposes the resulting set. |
| **8.2.2** | [#210](https://github.com/dex-it/dex-common/pull/210) (`7ff9bee`) | **API rewrite — biggest BC of the 8.2 line.** `IOutboxMessage.OutboxTypeId` is now `static abstract string` (was instance `string`). `IOutboxMessage.AllowAutoPublishing` is now `static virtual bool`. `IOutboxMessageHandler<T>.IsAutoPublisher` (`static virtual bool`, default `false`) added — auto-publishing handlers must override it to `true`; `PublisherOutboxHandler<T>` does this. The `new()` constraint introduced in 8.2.1 was **removed**. Requires C# 11 / .NET 7+. |
| 8.2.1 | [#209](https://github.com/dex-it/dex-common/pull/209) (`21f2e33`) | Short-lived `where TMessage : class, IOutboxMessage, new()` constraint on `IOutboxMessageHandler<T>`. Reverted in 8.2.2 — pin to 8.2.2+ to skip this. |
| **8.2** | [#208](https://github.com/dex-it/dex-common/pull/208) (`65b1878`) | `IOutboxTypeDiscriminatorProvider` introduced (auto-discovers all `IOutboxMessage` implementations in the current `AppDomain`). The previous standalone `IOutboxTypeDiscriminator` / `BaseOutboxTypeDiscriminator` types are **removed**. `DistributedEventOutboxDiscriminator` (multibus) is removed. Discriminator now lives on the message itself (`IOutboxMessage.OutboxTypeId`) rather than in a separate class — projects no longer need to register discriminators manually. |
| 1.9.x → main (post 8.x) | [#163/#165](https://github.com/dex-it/dex-common/pull/163) (`dd8fd2f`, Feb 2025) | `IOutboxMessage` moved to `Dex.Cap.Common.Interfaces`. `BaseOutboxMessage` deleted. Outbox can be safely shared between multiple services that consume different subsets of message types. |
| 1.9.x | [#145](https://github.com/dex-it/dex-common/pull/145) (`8f8bf18`, Apr 2024) | Discriminator support **first introduced** as a separate `IOutboxTypeDiscriminator` + `BaseOutboxTypeDiscriminator`. Before this release no discriminator logic existed; after — projects had to implement it explicitly. |

### Migration notes (8.1 → 8.2.2+)

1. Add `static abstract` to `OutboxTypeId` in every `IOutboxMessage` implementation:
   ```csharp
   // before
   public string OutboxTypeId => "…";
   // after
   public static string OutboxTypeId => "…";
   ```
2. If you used `AllowAutoPublishing`, mark it `static`:
   ```csharp
   public static bool AllowAutoPublishing => false;
   ```
3. Delete any custom `IOutboxTypeDiscriminator` / `BaseOutboxTypeDiscriminator` implementations and their DI registrations — they no longer exist.
4. If you wrote a custom auto-publishing handler (analogous to `PublisherOutboxHandler<>`), override `static IsAutoPublisher => true`. Regular per-type handlers should keep the default (`false`).
5. Bump straight to **8.2.2 or newer** — 8.2.0 / 8.2.1 had short-lived constraints that were rolled back.
