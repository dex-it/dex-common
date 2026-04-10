# Dex.Events

Распределённые события поверх MassTransit с опциональной Outbox-интеграцией.

## Dex.Events.Distributed

Публикация: `IDistributedEventRaiser<TBus>.RaiseAsync<T>(args, token)`.
Обработка: реализовать `IDistributedEventHandler<T>` (наследует MassTransit `IConsumer<T>`).

DI-регистрация:
```csharp
services.RegisterDistributedEventRaiser();                    // публикация
services.RegisterAllEventHandlers(assembly);                  // авто-обнаружение обработчиков
services.RegisterEventHandler<T>();                           // один обработчик
```

MassTransit endpoint:
```csharp
configurator.SubscribeEventHandlers<TEventParams, THandler>(); // до 3 обработчиков
```

Формат имени очереди: `Event_{ServiceName}_{QueueName}_{ConsumerName}`.

## Dex.Events.Distributed.OutboxExtensions

Транзакционная публикация событий через Outbox.
`OutboxDistributedEventMessage` оборачивает событие как `IOutboxMessage`.
Тип события хранится в `EventParamsType` (дискриминатор).

```csharp
services.RegisterOutboxDistributedEventHandler();
await outboxService.EnqueueEventAsync(outboxMessage, correlationId);
```

## Ограничения и gotchas

- `RaiseAsync` использует `as object` cast, что влияет на резолв типов в MassTransit
- Если дискриминатор пуст в outbox-сообщении, бросается `DiscriminatorResolveException`
- Assembly для авто-обнаружения обработчиков по умолчанию берётся из вызывающей сборки
