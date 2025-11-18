**Необходима установка плагина rabbitmq_delayed_message_exchange.**

## BaseConsumer
Можно использовать как базовый консьюмер для других консьюмеров если не нужна идемпотентность.
Умеет обрабатывать ошибки, пишет логи.
Умеет делать Defer - прерывает текущее исполнение путем выброса DeferConsumerException.
Отправляет сообщение в delay_exchange на указанный интервал.

## IdempotentConsumer
Наследует базовый консьюмер.
Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.

#### Ключ идемпотентности:
- по умолчанию значение поля IdempotentKey при реализации интерфейса IIdempotentKey
- если не реализован IIdempotentKey, то подставится MessageId из MassTransit.MessageContext

# Shared.Outbox

## PublisherOutboxHandler
Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события.

#### Регистрация
```csharp
services.AddOutbox<AppDbContext>();
services.AddOutboxPublisher();
```

## IdempotentOutboxHandler
Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.

#### Ключ идемпотентности:
- значение поля IdempotentKey при реализации интерфейса IIdempotentKey

#### Регистрация
```csharp
services.AddOnceExecutor<AppDbContext>();
services.AddScoped<IOutboxMessageHandler<MyCommand>, MyIdempotentOutboxHandler>();
```