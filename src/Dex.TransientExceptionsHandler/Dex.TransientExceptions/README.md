## TransientExceptions

Набор методов для конфигурации трансиентных ошибок (transient errors).
Можно использовать, например, при конфигурировании политик Polly, либо консьюмеров MassTransit.
Определен TransientExceptionsHandler для настройки определения трансиентных ошибок.

#### Стандартные конфигурации:

- TransientExceptionsHandler.Default (стандартная конфигурация, включает наиболее распространенные трансиентные ошибки)
- TransientExceptionsHandler.RetryAll (все ошибки будут считаться трансиентными)

#### Трансиентные ошибки в Default конфигурации:

- TimeoutException
- IOException
- SocketException
- OutOfMemoryException
- DbUpdateConcurrencyException
- OperationCanceledException
- RedisConnectionException
- RedisTimeoutException
- NpgsqlException (с флагом IsTransient)
- HttpRequestException (со статус-кодами 408, 429 и любым 5XX)
- Refit.ApiException (со статус-кодами 408, 429 и любым 5XX)
- RpcException (со статусами Unknown, Internal, Unavailable, Aborted, DeadlineExceeded, ResourceExhausted)
- WebException (со статусами ConnectFailure, Timeout, NameResolutionFailure, ProxyNameResolutionFailure, SendFailure, ReceiveFailure, KeepAliveFailure, PipelineFailure, ProtocolError, Pending)

#### Глобальные маркеры трансиентности:
- Все ошибки с интерфейсом-маркером ITransientException всегда будут безусловно трансиентными, независимо от конфигурации.
- Все ошибки с интерфейсом-маркером ITransientExceptionCandidate будут условно трансиентными, независимо от конфигурации. Условие трансиентности необходимо реализовать внутри ошибки, согласно интерфейсу.

#### Пример использования (для настройки консьюмера MassTransit)

```csharp
conf.AddConsumer<CustomConsumer>(configurator => configurator.UseRedeliveryRetryConfiguration(TransientExceptionsHandler.Default));
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