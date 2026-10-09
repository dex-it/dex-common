# Dex.TransientExceptionsHandler

Конфигурируемый определитель transient-ошибок для Polly/MassTransit retry.

## Готовые конфигурации

- `TransientExceptionsHandler.Default`: стандартный набор (рекомендуется)
- `TransientExceptionsHandler.RetryAll`: все исключения считаются transient (для тестов)

## Default покрывает

По типу (с наследованием): TimeoutException, IOException, SocketException, OutOfMemoryException, DbUpdateConcurrencyException, OperationCanceledException, RedisConnectionException, RedisTimeoutException, Polly `ExecutionRejectedException` — все отказы Polly: таймаут, circuit breaker, rate limiter, bulkhead (тип Polly.Core; отказы Polly 8.x наследуют от него, Polly 7.x — другие типы, не покрываются).

По предикату (с наследованием): NpgsqlException (только IsTransient=true, в том числе PostgresException), HttpRequestException (408, 429, 5xx), Refit.ApiException (408, 429, 5xx), RpcException (Unknown, Internal, Unavailable, Aborted, DeadlineExceeded, ResourceExhausted), WebException (ConnectFailure, Timeout, и др.).

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
- Предикат ищется по цепочке базовых типов исключения: срабатывает на наследниках; из нескольких предикатов на цепочке достаточно одного true
- Polly подключён пакетом `Polly.Core`, а не сравнением имён типов: выбрана проверка по типу; цена — Polly 7.x не покрыт
- `PostgresException.IsTransient` (Npgsql 8) не включает `57014` (statement_timeout): такой таймаут не повторяется, это решение Npgsql
- `ConsumerCanceledException` MassTransit пакет не знает: перевод в `OperationCanceledException` делает `HandleTransient` из Dex.MassTransit.Rabbit
- InnerException проверяются до указанной глубины (default 10; само исключение — первый уровень, 0 и 1 — без вложенных): любое совпадение делает внешнее исключение transient
- Обход вложенных — свой (`EnumerateInnerExceptions`), в глубину, с заходом во все `AggregateException.InnerExceptions`; `GetInnerExceptions` из Dex.Extensions идёт только по `InnerException`. Ссылка на Dex.Extensions оставлена: потребители могли получать его транзитивно
