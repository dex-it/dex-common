# Dex.MassTransit

Обёртки над MassTransit для RabbitMQ и AWS SQS с OpenTelemetry tracing.

## Структура

- `Dex.MassTransit`: базовый пакет, `QueueNameConventionHelper` (убирает суффикс "Dto" из имён)
- `Dex.MassTransit.Rabbit`: конфигурация шины для RabbitMQ
- `Dex.MassTransit.SQS`: конфигурация шины для AWS SQS
- `Dex.MassTransit.ActivityTrace`: пропагация Activity.TraceId через пайплайн MassTransit

## RabbitMQ

Конфигурация через `MassTransitConfigurator` с generic-опциями `TMqOptions : RabbitMqOptions`.
`RabbitMqOptions`: Host, Port, VHost, Username, Password, IsSecure, CertificatePath.
Методы: `RegisterBus<>()`, `RegisterReceiveEndpoint<>()`, `RegisterSendEndPoint<>()`.
`BaseConsumer<TMessage>`: абстрактный базовый consumer с обработкой ошибок и `Defer()`.
Запись об ошибке — `ILogger.LogConsumeError`: тело сообщения усекается до `MessageDataLimit`; если в контейнере консьюмера
зарегистрирован `IMessageDataMasker` (берётся из payload `IServiceProvider` контекста), он получает вход с запасом
`MaskerInputFactor` над лимитом; результат длиннее лимита пакет обрезает сам — потолок записи за пакетом. Готовый
`SensitiveNamesMessageDataMasker` пакет не регистрирует.
Retry-конфигурация: `UseRedeliveryRetryConfiguration()`, `UseRetryConfiguration()`, `UseLimitPrefetchConfiguration()`.

## SQS

`AmazonMqOptions`: Region, AccessKey, SecretKey, OwnerId.
FIFO-очереди: имена DTO должны заканчиваться на "Fifo".
Дедупликация: CorrelationId + фиксированный GroupId.
Prefetch: 10 (max), WaitTimeSeconds: 20.

## Activity Tracing

```csharp
configurator.LinkActivityTracingContext(); // включено по умолчанию
```

Добавляет ActivityTracingPipeSpecification в Consume, Send и Publish пайплайны.
Отключение: `EnableConsumerTracer = false`.

## Ограничения и gotchas

- UseDelayedRedelivery ОБЯЗАТЕЛЬНО вызывать ДО UseMessageRetry (порядок критичен)
- Не использовать `concurrencyLimit=1 + prefetchCount=1` с Redelivery: ломает порядок сообщений
- SQS FIFO: имена DTO обязаны заканчиваться на "Fifo"
- BaseConsumer.Defer() бросает internal exception для пропуска логирования ошибки
- Сбой маскировщика или его разрешения из контейнера тело не открывает: в `MessageData` уходит `<not masked: Тип>`
- Маскировщик получает только консьюмер из контейнера: зарегистрированный фабрикой или экземпляром пишет тело без маски, если
  endpoint не открывает scope сообщения (`UseMessageScope`)
- У `SensitiveNamesMessageDataMasker` публичный конструктор только без параметров, свой список — через `Create`: конструктор со
  списками контейнер выбрал бы как самый полный и подставил бы пустые `IEnumerable<string>`
