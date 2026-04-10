# Dex.Cap

Два паттерна транзакционной надёжности: Outbox и OnceExecutor. Общий код в `Dex.Cap.Common` и `Dex.Cap.Common.Ef`.

## Outbox (транзакционная публикация сообщений)

Сообщение должно реализовать `IOutboxMessage` со статическим свойством `OutboxTypeId`.
Флаг `AllowAutoPublishing` (default true) включает авто-отправку через `AddOutboxPublisher()`.
Флаг `DeleteImmediately` удаляет сообщение сразу после обработки.

Постановка в очередь: `IOutboxService.EnqueueAsync<T>()` с опциональным correlationId, scheduledStartDate, lockTimeout.
Проверка наличия операции: `IsOperationExistsAsync(correlationId)`.
Обработка: фоновый `AddDefaultOutboxScheduler<TDbContext>(periodSeconds, cleanupDays)`.
Явные обработчики: `IOutboxMessageHandler<T>`, регистрация через `services.AddScoped<IOutboxMessageHandler<TMessage>, THandler>()`.

## OnceExecutor (идемпотентное выполнение)

Два варианта: базовый `IOnceExecutor<TOptions>` и стратегический `IStrategyOnceExecutor<TArg, TResult>`.
Стратегия реализует `IOnceExecutionStrategy<TArg, TResult>` с методами: `IsAlreadyExecuted`, `Execute`, `Read`.
Очистка ключей: `AddDefaultOnceExecutorScheduler<TDbContext>(periodSeconds, cleanupDays)`.

## DI-регистрация

```csharp
services.AddOutbox<TDbContext>();
services.AddDefaultOutboxScheduler<TDbContext>(periodSeconds: 5, cleanupDays: 7);
services.AddOutboxPublisher();  // авто-обработчик для AllowAutoPublishing=true

services.AddOnceExecutor<TDbContext>();
services.AddStrategyOnceExecutor<TArg, TResult, TStrategy, TDbContext>();
```

## EF-конфигурация

```csharp
modelBuilder.OutboxModelCreating();       // таблица OutboxEnvelope
modelBuilder.OnceExecutorModelCreating(); // таблица LastTransaction
```

## Провайдеры

Outbox: EF Core (`Dex.Cap.Outbox.Ef`), Neo4j (`Dex.Cap.Outbox.Neo4j`).
OnceExecutor: EF Core, ClickHouse, Neo4j, Memory (для тестов).
MassTransit-интеграция: `Dex.Cap.Outbox.OnceExecutor.MassTransit`.

## Ограничения и gotchas

- PostgreSQL + xmin concurrency: исключить `OutboxEnvelope` и `LastTransaction` из `UseXminAsConcurrencyToken`
- Lock timeout по умолчанию 30 сек (минимум 10 сек), должен превышать время обработки сообщения
- Scheduler автоматически регистрирует health check
- OnceExecutor привязан к idempotency key: повторный вызов с тем же ключом вернёт результат первого выполнения
