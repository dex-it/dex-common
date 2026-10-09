# Dex.TransientExceptionsHandler

Конфигурируемый определитель transient-ошибок для Polly/MassTransit retry.

## Готовые конфигурации

- `TransientExceptionsHandler.Default`: стандартный набор (рекомендуется)
- `TransientExceptionsHandler.RetryAll`: все исключения считаются transient (для тестов)

## Default покрывает

По типу (с наследованием): TimeoutException, IOException, SocketException, OutOfMemoryException, DbUpdateConcurrencyException, OperationCanceledException, RedisConnectionException, RedisTimeoutException.

По полному имени типа на цепочке наследования: `Polly.ExecutionRejectedException` — все отказы Polly 7.x и 8.x: таймаут, circuit breaker, rate limiter, bulkhead.

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
- Polly — по имени типа, без ссылки на пакет: `Polly.ExecutionRejectedException` лежит в Polly.dll у 7.x и в Polly.Core у 8.x; ссылка на Polly.Core давала потребителю на Polly 7.x (его тянет `Microsoft.Extensions.Http.Polly` 8.0.x) CS0433 на `TimeoutRejectedException` и прочих. Тесты идут на обеих настоящих сборках: Polly 7 подключён в тестовый проект через `extern alias PollyV7`
- `PostgresException.IsTransient` (Npgsql 8) не включает `57014` (statement_timeout): такой таймаут не повторяется, это решение Npgsql
- `ConsumerCanceledException` MassTransit пакет не знает: перевод в `OperationCanceledException` делает `HandleTransient` из Dex.MassTransit.Rabbit
- InnerException проверяются до указанной глубины (default 10; само исключение — первый уровень, 0 и 1 — без вложенных): любое совпадение делает внешнее исключение transient
- Маркеры (`MarkerCheck`): первый на пути по `InnerException` решает финально, в том числе против совпадений по типу выше него. На `AggregateException` ветки независимы (`AggregateMarkerCheck`): есть маркер хоть в одной — каждая ветка проверяется целиком на оставшуюся глубину, итог any-true; маркеров нет — решают типы по всему дереву. Иначе исход зависел бы от порядка веток, а `Candidate(false)` в соседней ветке гасил бы таймаут
- Обход вложенных для типов и предикатов — свой (`EnumerateInnerExceptions`), в глубину, с заходом во все `AggregateException.InnerExceptions`; `GetInnerExceptions` из Dex.Extensions идёт только по `InnerException`. Ссылка на Dex.Extensions оставлена: потребители могли получать его транзитивно
