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
В Dex.TransientExceptions определен TransientExceptionsHandler для настройки определения трансиентных ошибок.
Стандартные конфигурации:

- TransientExceptionsHandler.Default
- TransientExceptionsHandler.RetryAll

##### Стандартные трансиентные ошибки в Default конфигурации:

- TimeoutException
- IOException
- SocketException
- OutOfMemoryException
- DbUpdateConcurrencyException
- OperationCanceledException
- RedisConnectionException
- RedisTimeoutException
- NpgsqlException (с флагом IsTransient)
- HttpRequestException (со статусом кодами 408, 429 и любым 5XX)
- Refit.ApiException (со статусом кодами 408, 429 и любым 5XX)
- RpcException (со статусом Unknown, Internal, Unavailable, Aborted, DeadlineExceeded, ResourceExhausted)
- WebException (со статусом ConnectFailure, Timeout, NameResolutionFailure, ProxyNameResolutionFailure, SendFailure, ReceiveFailure, KeepAliveFailure,
  PipelineFailure, ProtocolError, Pending)

##### Глобальные метки трансиентности
- Все ошибки с интерфейсом-меткой ITransientException всегда будут безусловно трансиентными, независимо от конфигурации.
- Все ошибки с интерфейсом-меткой ITransientExceptionCandidate будут условно трансиентными, независимо от конфигурации. Условие трансиентности необходимо реализовать внутри ошибки, согласно интерфейсу.

### Пример использования

```csharp
conf.AddConsumer<CustomConsumer>(configurator => configurator.UseDefaultConfiguration(TransientExceptionsHandler.Default, 1));
```

Если стандартные конфигурации определения трансиентных ошибок не подходят, можно определить кастомную:

#### Пример добавления TransientExceptionsHandler.Custom

```csharp
/// <summary>
/// Для лучшей производительности рекомендуется единожды выполнить конфигурацию BuildCustomHandler()
/// После чего, сохранить готовый к использованию экземпляр в статическом поле и передать во все использующие его флоу
/// </summary>
public static TransientExceptionsHandler Custom { get; } = BuildCustomHandler();

private static TransientExceptionsHandler BuildCustomHandler()
{
    var builder = new TransientExceptionsHandler();

    // добавление безусловной кастомной трансиентной ошибки (включая наследников)
    builder.Add(typeof(EntityNotFoundException));

    // добавление кастомной проверки на трансиентность ошибки
    builder.Add<SomeException>(exception => exception is SomeException {Message: "Is some transient exception"});

    // конфигурирование глубины проверки InnerExceptions
    // любая найденная InnerException подходящая под описанные выше правила, делает входящую ошибку transient
    // при значении 0, InnerExceptions не будут проверяться
    // НЕОБЯЗАТЕЛЬНО, так как по умолчанию уже установлена не-нулевая глубина проверки
    builder.SetInnerExceptionsSearchDepth(99);

    // отключает стандартные проверки из TransientExceptionsHandler.Default конфига, оставляя только те, которые были явно добавлены в текущий билдер
    // НЕОБЯЗАТЕЛЬНО: по умолчанию, если этот метод не был вызван, все явно добавленные ошибки будут дополнены и теми, что уже были определены в TransientExceptionsHandler.Default
    builder.DisableDefaultBehaviour();

    // ОБЯЗАТЕЛЬНО: требуется явно вызвать Build после завершения конфигурирования, до начала использования
    return builder.Build();
}
```
#### Альтернативный пример быстрого добавления TransientExceptionsHandler.DefaultEntityNotFound
```csharp
// В этом примере создается конфиг, включающий дефолтные настройки TransientExceptionsHandler.Default + 1 новая безусловно трансиентная ошибка (включая наследников): EntityNotFoundException
    
public static TransientExceptionsHandler DefaultEntityNotFound { get; } = new([typeof(EntityNotFoundException)], disableDefaultBehaviour: false, runBuild: true);
```

#### Глобальное конфигурирование трансиентности ошибок
```csharp
/// <summary>
/// Всегда трансиентная ошибка
/// </summary>
public class TransientException : Exception, ITransientException;

/// <summary>
/// Условно-трансиентная ошибка
/// </summary>
public class MayBeTransientException : Exception, ITransientExceptionCandidate
{
    /// <summary>
    /// Условие трансиентности
    /// </summary>
    public bool IsTransient => Message.Contains("some transient code mark", StringComparison.InvariantCulture);
}
```

# Shared.Outbox

## PublisherOutboxHandler

Автоматически публикует объект, из аутбокса в очередь, заинтересованные сервисы могут получать эти события.

### Регистрация

```csharp
services.AddOutbox<AppDbContext>();

// простой универсальный вариант для автоматической публикации, рекомендуемый вариант
services.AddOutboxPublisher();

// альтернативный вариант с явной реализацией хендлера
services.AddScoped<IOutboxMessageHandler<SomeOutboxMessage>, SomeOutboxMessageHandler>();
```

### Отправка

Сообщение SomeOutboxMessage отправляется в бд для дальнейшей обработки

```csharp
public class SomeService(IOutboxService outbox, IUnitOfWork dbContext)
{
    public async Task Example()
    {
        await outbox.EnqueueAsync(new SomeOutboxMessage
        {
            SomeData = 123
        });

        await dbContext.SaveChangesAsync();
    }
}
```

### IOutboxMessage
Сообщения аутбокса должны реализовывать интерфейс IOutboxMessage:

####  OutboxTypeId - обязательно, string. 
Уникальный идентификатор типа сообщения.

#### AllowAutoPublishing - необязательно, bool, по умолчанию true.
Разрешает сообщению автоматически публиковаться с помощью ```services.AddOutboxPublisher();```
При значении false будет требовать явной реализации отдельного хендлера ```services.AddScoped<IOutboxMessageHandler<SomeOutboxMessage>, SomeOutboxMessageHandler>();```

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