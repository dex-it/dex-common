# Dex.TransientExceptionsHandler

Конфигурируемый определитель transient-ошибок для Polly/MassTransit retry.

## Готовые конфигурации

- `TransientExceptionsHandler.Default`: стандартный набор (рекомендуется)
- `TransientExceptionsHandler.RetryAll`: все исключения считаются transient (для тестов)

## Default покрывает

По типу (с наследованием): TimeoutException, IOException, SocketException, OutOfMemoryException, DbUpdateConcurrencyException, OperationCanceledException, RedisConnectionException, RedisTimeoutException.

По предикату: NpgsqlException (только IsTransient=true), HttpRequestException (408, 429, 5xx), Refit.ApiException (408, 429, 5xx), RpcException (Unknown, Internal, Unavailable, Aborted, DeadlineExceeded, ResourceExhausted), WebException (ConnectFailure, Timeout, и др.).

## Глобальные маркеры

- `ITransientException`: безусловно transient (обходит конфигурацию)
- `ITransientExceptionCandidate`: условно transient (свойство `bool IsTransient`)

## Builder-паттерн

```csharp
var custom = new TransientExceptionsHandler()
    .Add(typeof(CustomException))                              // по типу (с наследованием)
    .Add<SomeException>(ex => ex.Message.Contains("retry"))    // с предикатом
    .SetInnerExceptionsSearchDepth(5)                          // default: 10
    .Build();                                                  // ОБЯЗАТЕЛЬНО перед использованием

var noDefaults = new TransientExceptionsHandler()
    .DisableDefaultBehaviour()                                 // убрать стандартные проверки
    .Add(typeof(CustomException))
    .Build();
```

## Интеграция

```csharp
// Polly
Policy.Handle<Exception>(TransientExceptionsHandler.Default).WaitAndRetryAsync(...);

// MassTransit
configurator.UseRedeliveryRetryConfiguration(TransientExceptionsHandler.Default);

// Прямая проверка
if (handler.Check(exception)) { /* retry */ }
```

Неявное преобразование в `Func<Exception, bool>`.

## Ограничения и gotchas

- `Build()` ОБЯЗАТЕЛЕН перед `Check()` (иначе InvalidOperationException)
- После `Build()` экземпляр заморожен: Add/Disable бросают InvalidOperationException
- Предикаты проверяются по точному типу (не по наследованию); для наследования использовать `Add(Type)`
- InnerException проверяются до указанной глубины (default 10): любое совпадение делает внешнее исключение transient
