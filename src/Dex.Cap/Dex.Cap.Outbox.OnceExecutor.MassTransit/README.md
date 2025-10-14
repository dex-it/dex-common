**Необходима установка плагина rabbitmq_delayed_message_exchange.**

## BaseConsumer

Можно использовать как базовый консьюмер для других консьюмеров если не нужна идемпотентность.
Умеет обрабатывать ошибки, пишет логи.
Умеет делать Defer - прерывает текущее исполнение путем выброса DeferConsumerException.
Отправляет сообщение в delay_exchange на указанный интервал.

## IdempotentConsumer

Наследует базовый консьюмер.
Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.

Ключ идемпотентности:
- по умолчанию будет использовано значение поля MessageContext.MessageId
- можно задать свой ключ через реализацию интерфейса IHaveIdempotenceKey

### Регистрация
```csharp
services.AddOutbox<AppDbContext>();

services.AddOnceExecutor<AppDbContext>();
```

## MassTransitConfigurationExtensions

Набор экстеншенов для конфигурации MassTransit.
Конфигурация по умолчанию позволяет задать Delay и Retry настройки, срабатывающие при трансиентных ошибках.
В BaseStartup определен виртуальный метод CheckTransientException, и список трансиентных ошибок.
По умолчанию считаем ВСЕ ошибки трансиентными.

### Пример использования
```csharp
conf.AddConsumer<CustomConsumer>(configurator => configurator.UseDefaultConfiguration(CheckTransientException, 1));
```

# Shared.Outbox

## PublisherOutboxHandler

Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события.

### Регистрация
```csharp
services.AddOutbox<AppDbContext>();

services.AddOutboxPublisher();
```

## IdempotentOutboxHandler

Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.

Ключ идемпотентности:
- значение поля BaseOutboxMessage.MessageId

### Регистрация
```csharp
services.AddOutbox<AppDbContext>();

services.AddOnceExecutor<AppDbContext>();

services.AddScoped<IOutboxMessageHandler<SendNotificationCommand>, SendNotificationOutboxHandler>();
```