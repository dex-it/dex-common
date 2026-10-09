# Dex.MassTransit

Helpers and conventions on top of [MassTransit](https://masstransit.io/) for RabbitMQ and Amazon SQS:

| Package | Purpose |
|---|---|
| `Dex.MassTransit` | Shared queue-name convention (`<MessageType>` with the `Dto` suffix stripped). |
| `Dex.MassTransit.Rabbit` | RabbitMQ bus registration, send/receive endpoint mapping, `BaseConsumer<T>` with `Defer`, retry/redelivery helpers. |
| `Dex.MassTransit.SQS` | Amazon SQS bus registration, FIFO support, deduplication helpers. |
| `Dex.MassTransit.ActivityTrace` | `Activity.TraceId` propagation across producers and consumers via a pipe specification (`MT-Activity-Id` header). |

---

# Dex.MassTransit.Rabbit

### Configure options and register the bus

```csharp
services.Configure<RabbitMqOptions>(opt =>
{
    opt.Host = "localhost";
    opt.Port = 5672;
    opt.VHost = "/";
    opt.Username = "guest";
    opt.Password = "guest";
});

services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<HelloConsumer>();

    configurator.RegisterBus((context, factory) =>
    {
        // Receive endpoint: queue name is derived from TMessage (HelloMessageDto → "HelloMessage").
        context.RegisterReceiveEndpoint<HelloMessageDto, HelloConsumer>(factory);

        // Optional: a dedicated queue for THIS consumer only (pub-sub fan-out).
        context.RegisterReceiveEndpoint<HelloMessageDto, HelloConsumer>(factory, createSeparateQueue: true);
    });
});
```

For send-only services:

```csharp
services.AddMassTransit(configurator =>
{
    configurator.RegisterBus((context, _) =>
    {
        context.RegisterSendEndPoint<HelloMessageDto>();
    });
});
```

`RegisterSendEndPoint<TMessage>` calls `EndpointConvention.Map<TMessage>(...)` so `IPublishEndpoint.Send<TMessage>(...)` knows the address without an explicit URI.

### Multiple buses

Use a marker `IBus`-derived interface and a derived options class to isolate connections:

```csharp
public interface IOtherRabbitMqBus : IBus { }
public class OtherRabbitMqOptions : RabbitMqOptions { }

services.Configure<OtherRabbitMqOptions>(opt => { opt.Host = "other-host"; });
services.AddMassTransit<IOtherRabbitMqBus>(configurator =>
{
    configurator.AddConsumer<OtherConsumer>();
    configurator.RegisterBus<OtherRabbitMqOptions>((context, factory) =>
    {
        context.RegisterReceiveEndpoint<OtherRabbitMqOptions, OtherMessageDto, OtherConsumer>(factory);
    });
});
```

### Refresh connection callback

`RegisterBus(..., refreshConnectCallback: ...)` lets you mutate `ConnectionFactory` (e.g. rotate the password from a token service) **before each reconnect** without restarting the host:

```csharp
configurator.RegisterBus((context, factory) =>
{
    /* register endpoints */
}, refreshConnectCallback: ctx =>
{
    var tokenSvc = ctx.GetRequiredService<ITestPasswordService>();
    return async f => f.Password = await tokenSvc.GetAccessToken();
});
```

### Saga send endpoints

```csharp
provider.RegisterSendEndPoint<TCommand, TSagaInstance>();
```

Maps `TCommand` to the saga state-machine endpoint instead of a regular consumer queue.

### BaseConsumer&lt;T&gt;

Optional base class for consumers. Adds logging on unhandled exceptions and supports **`Defer`** — postpone the message via `ConsumeContext.Defer(delay)` (RabbitMQ delayed exchange) and exit silently:

```csharp
public class PaymentConsumer(ILogger<PaymentConsumer> log) : BaseConsumer<PaymentDto>(log)
{
    protected override async Task Process(ConsumeContext<PaymentDto> context)
    {
        if (!_gatewayReady)
            await Defer(TimeSpan.FromMinutes(5)); // throws DeferConsumerException (caught internally)

        await HandleAsync(context.Message);
    }
}
```

> Requires the RabbitMQ plugin **`rabbitmq_delayed_message_exchange`** for `Defer` to work.

### Error record and message body masking

On failure `BaseConsumer<T>` writes one error record via `ILogger.LogConsumeError` (a consumer of several message types calls it from its own `catch`): message type, `MessageId`, `ConversationId`, retry attempt and the message body as a single JSON string, truncated to `MessageDataLimit` bytes of UTF-8 (4000 by default, overridable).

The body is written as is unless an `IMessageDataMasker` is registered in the container the consumers are resolved from — consumers themselves stay unchanged. The package does **not** register one. A ready-made masker by field names is included; register it yourself:

```csharp
// default names: fragments password, passwd, pwd, secret, token, apikey, authorization, credential, privatekey, cvv, cvc
// match anywhere in the name (case and any character other than a letter or digit are ignored: CardCvv, x-api-key, User:Password);
// words pin, otp match a whole word of the name or its plural (PinCode, SmsOtp, PINCode, PINs, Card:Pin — but not Shipping, RootPath);
// a word entry of several words (PayerBic, pin_code) matches the whole name in any form (payer_bic, PAYERBIC)
services.AddSingleton<IMessageDataMasker, SensitiveNamesMessageDataMasker>();

// or your own list instead of the default one
services.AddSingleton<IMessageDataMasker>(SensitiveNamesMessageDataMasker.Create(
    nameFragments: ["password", "token", "iban"],
    nameWords: ["pin"]));
```

A value under a matching name — string, number, object or array — is replaced with `"***"` as a whole and never reaches the record even partially, including one cut off at the end of the input. An ordinary string that does not fit the limit is cut on a character boundary, as without a masker — unless it was already cut off at the end of the masker's input (roughly longer than three times the limit): such a string is dropped. The masker looks at field names only: a name held in a neighbouring field (a list of `Key`/`Value` pairs) and JSON inside a string value are not masked. Entries of your own list are normalized the same way as names, so `" token"` from a `Split(',')` still matches. To apply your own rules, implement the interface and register it the same way:

```csharp
public sealed class MyMasker : IMessageDataMasker
{
    // json: the serialized body or its beginning, cut on a character boundary;
    // isComplete == false means only the beginning of the body was passed.
    // Honour isComplete: a value cut off at the end of the input must not be written even partially.
    // Keep the result within `limit` bytes of UTF-8 plus the truncation mark, and mark an incomplete result yourself.
    public string Mask(ReadOnlySpan<byte> json, bool isComplete, int limit) => ...;
}
```

With a masker the package serializes up to four times `MessageDataLimit` and hands that to the masker, so the space freed by masked values goes to the following fields; the whole message graph is still never walked. The record ceiling stays with the package: a result longer than `limit` bytes (plus the `...` mark) is cut to `limit` on a character boundary and marked, and `null` is written as an empty body. A result within the limit is written as is. Only consumers resolved from the container get the masker: a consumer registered by factory or instance (`e.Consumer(() => new ...)`, `Instance`, `Handler`) writes the body unmasked, unless the endpoint opens a message scope with `UseMessageScope(context)`. If the masker (or its resolution) throws, the record gets `<not masked: ExceptionType>` instead of the body, and the original consumer exception is kept.

### Retry and redelivery

Two extension methods on `IConsumerConfigurator<TConsumer>` and one on the retry configurator:

| Method | Effect |
|---|---|
| `HandleTransient(checkTransient)` | On `IExceptionConfigurator` (inside `UseMessageRetry` / `UseDelayedRedelivery`): retries what `checkTransient` accepts, with the MassTransit exception mapping described below. |
| `UseRetryConfiguration(checkTransient, retryLimit?, retryIntervals?)` | In-process exponential retry (defaults: 3 attempts, `1s`–`5s` with `1s` delta). |
| `UseRedeliveryRetryConfiguration(checkTransient, retryLimit?, retryIntervals?, redeliveryIntervals?)` | Delayed redelivery via the broker **followed by** in-process retry (defaults: 5 min, 15 min, 30 min, 1 h, 3 h, 6 h). |

```csharp
configurator.RegisterReceiveEndpoint<PaymentDto, PaymentConsumer>(factory, endpoint =>
{
    endpoint.UseRedeliveryRetryConfiguration(
        checkTransientException: TransientExceptionsHandler.Default,
        retryIntervals: new RetryExponentialIntervals(
            MinInterval: TimeSpan.FromSeconds(2),
            MaxInterval: TimeSpan.FromSeconds(30),
            Delta:       TimeSpan.FromSeconds(2)));
});
```

Pair the `checkTransientException` callback with [`Dex.TransientExceptions`](https://github.com/dex-it/dex-common) for a project-wide policy of which errors are considered transient.

MassTransit replaces an `OperationCanceledException` thrown by a consumer, a handler, a saga or a `UseTimeout` filter while the bus is running (for example, an `HttpClient.Timeout`) with `ConsumerCanceledException`, and drops the original exception, before the retry filters see it. Both methods first pass the exception to `checkTransientException` as is; if the policy rejects it, they pass `ConsumerCanceledException` again as an `OperationCanceledException` and `RequestTimeoutException` as a `TimeoutException`, with the MassTransit exception as the `InnerException`. As a result, a cancellation inside a consumer is retried when the policy retries either `ConsumerCanceledException` or `OperationCanceledException` (`TransientExceptionsHandler.Default` does). A consumer that cancels on purpose to skip a message is retried as well.

The same mapping is available for any retry or redelivery — a handler, a saga, an endpoint — through `HandleTransient` on the retry configurator. A plain `r.Handle(...)` bypasses it.

```csharp
endpoint.UseMessageRetry(r =>
{
    r.Intervals(100, 500, 1000);
    r.HandleTransient(TransientExceptionsHandler.Default);
});
```

MassTransit decides whether to publish `Fault<T>` after each attempt by asking the policy about the consumer's original exception, before the substitution. A policy that rejects the original exception but accepts the final one (for example, `ex is ConsumerCanceledException`, or a cancellation wrapped in another exception with a policy that does not look at inner exceptions) gets a `Fault<T>` for every attempt rather than one after the last.

### Concurrency / prefetch

```csharp
endpoint.UseLimitPrefetchConfiguration(concurrencyLimit: 1, prefetchCount: 1);
```

> **Do not** combine `UseLimitPrefetchConfiguration(1, 1)` with `UseRedeliveryRetryConfiguration` — the broker-side redelivery removes the message from the queue and the consumer immediately picks up the next one, so strict in-order processing is no longer guaranteed.

### Queue naming convention

`QueueNameConventionHelper.GetOnlyQueueName<TMessage>()` produces the queue name by stripping a trailing `Dto` (case-insensitive) from the type name: `HelloMessageDto → HelloMessage`. For per-consumer queues (`createSeparateQueue: true`) the format is `{ServiceName}_{QueueName}_{ConsumerType}` (where `ServiceName` defaults to the entry assembly name unless an explicit `serviceName` is passed).

### TLS

```csharp
services.Configure<RabbitMqOptions>(opt =>
{
    opt.IsSecure = true;
    opt.CertificatePath = "/etc/ssl/rabbit.pfx";
});
```

---

# Dex.MassTransit.SQS

Similar API on top of `MassTransit.AmazonSQS`.

```csharp
services.Configure<AmazonMqOptions>(opt =>
{
    opt.Region    = "eu-west-1";
    opt.AccessKey = "...";
    opt.SecretKey = "...";
    opt.OwnerId   = "123456789012"; // AWS account id, required
});

services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderFifoConsumer>();
    configurator.RegisterBus((context, factory) =>
    {
        // Standard SQS queue
        context.RegisterReceiveEndpoint<EventDto, EventConsumer>(factory);

        // FIFO queue (DTO name MUST end with "Fifo")
        context.RegisterReceiveEndpointAsFifo<OrderProcessingFifo, OrderFifoConsumer>(factory);
    });
});
```

FIFO send-side helper that sets `GroupId` and a `DeduplicationId` derived from `CorrelationId`:

```csharp
configurator.ConfigureSend.ConfigureSendEndpointAsFifoForTypes([typeof(OrderProcessingFifo)]);
```

> `RegisterReceiveEndpointAsFifo` throws `ConfigurationException` if the DTO type name doesn't end with `Fifo` — the suffix is mandatory because AWS requires `.fifo` in the queue name.

---

# Dex.MassTransit.ActivityTrace

Propagates `Activity.Current?.Id` from producer to consumer in the `MT-Activity-Id` header. Enabled **automatically** for buses registered via `RegisterBus` (controlled by `MassTransitConfigurator.EnableConsumerTracer`, default `true`):

```csharp
MassTransitConfigurator.EnableConsumerTracer = false; // opt out before RegisterBus
```

To wire it into a bus that is not registered through `Dex.MassTransit.Rabbit`:

```csharp
busFactoryConfigurator.LinkActivityTracingContext();
```

This registers the pipe specification on consume, send and publish pipelines.

---

# Breaking changes

| Version | PR / Commit | Change |
|---|---|---|
| **8.1.0** | [#248](https://github.com/dex-it/dex-common/issues/248) | `UseRetryConfiguration` / `UseRedeliveryRetryConfiguration` now retry a `ConsumerCanceledException` (a cancellation or `UseTimeout` inside the consumer) and a `RequestTimeoutException` when the policy accepts `OperationCanceledException` / `TimeoutException`. With `TransientExceptionsHandler.Default` such messages are retried and redelivered (up to the whole redelivery window) instead of going to `_error` on the first attempt, including a consumer that throws `OperationCanceledException` on purpose to skip a message. New `HandleTransient` on the retry configurator. |
| **8.0.11+** | [#199](https://github.com/dex-it/dex-common/pull/199) (`92c3e8b`) | Bumped to MassTransit **8.5.3**. Review your consumer signatures and middleware against the upstream changelog. |
| 8.0.7+ | [#203](https://github.com/dex-it/dex-common/pull/203) (`9f78bd4`) | MassTransit packages bumped together with `Dex.Cap.Outbox` `AddOutboxPublisher()`. No source-level break in `Dex.MassTransit.*`. |
| `13eda98` | local feat | `UseRedeliveryRetryConfiguration` and `UseRetryConfiguration` gained a new `RetryExponentialIntervals? retryIntervals = null` optional parameter. The default `(1s, 5s, 1s)` is preserved, but **named-argument** callers should double-check argument order. |
| `4c3c621` ([#156](https://github.com/dex-it/dex-common/pull/156), Aug 2024) | API rewrite | `Dex.MassTransit.Rabbit` switched to the modern surface: `BaseConsumer<T>` introduced, retry/redelivery extensions added, `RegisterReceiveEndpoint`/`RegisterSendEndPoint` reordered generic parameters (`TMqOptions, TMessage, TConsumer`), removed `concurrencyLimit` parameter, removed default `AutoDelete`/`Durable` overrides on bus configuration. Source-incompatible with pre-8.x registrations — update call sites accordingly. |
