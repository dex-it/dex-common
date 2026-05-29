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

### Retry and redelivery

Two extension methods on `IConsumerConfigurator<TConsumer>`:

| Method | Effect |
|---|---|
| `UseRetryConfiguration(checkTransient, retryLimit?, retryIntervals?)` | In-process exponential retry (defaults: 3 attempts, `1s`–`5s` with `1s` delta). |
| `UseRedeliveryRetryConfiguration(checkTransient, retryLimit?, retryIntervals?, redeliveryIntervals?)` | Delayed redelivery via the broker **followed by** in-process retry (defaults: 5 min, 15 min, 30 min, 1 h, 3 h, 6 h). |

```csharp
configurator.RegisterReceiveEndpoint<PaymentDto, PaymentConsumer>(factory, endpoint =>
{
    endpoint.UseRedeliveryRetryConfiguration(
        checkTransientException: ex => ex is HttpRequestException or TimeoutException,
        retryIntervals: new RetryExponentialIntervals(
            MinInterval: TimeSpan.FromSeconds(2),
            MaxInterval: TimeSpan.FromSeconds(30),
            Delta:       TimeSpan.FromSeconds(2)));
});
```

Pair the `checkTransientException` callback with [`Dex.TransientExceptions`](https://github.com/dex-it/dex-common) for a project-wide policy of which errors are considered transient.

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
| **8.0.11+** | [#199](https://github.com/dex-it/dex-common/pull/199) (`92c3e8b`) | Bumped to MassTransit **8.5.3**. Review your consumer signatures and middleware against the upstream changelog. |
| 8.0.7+ | [#203](https://github.com/dex-it/dex-common/pull/203) (`9f78bd4`) | MassTransit packages bumped together with `Dex.Cap.Outbox` `AddOutboxPublisher()`. No source-level break in `Dex.MassTransit.*`. |
| `13eda98` | local feat | `UseRedeliveryRetryConfiguration` and `UseRetryConfiguration` gained a new `RetryExponentialIntervals? retryIntervals = null` optional parameter. The default `(1s, 5s, 1s)` is preserved, but **named-argument** callers should double-check argument order. |
| `4c3c621` ([#156](https://github.com/dex-it/dex-common/pull/156), Aug 2024) | API rewrite | `Dex.MassTransit.Rabbit` switched to the modern surface: `BaseConsumer<T>` introduced, retry/redelivery extensions added, `RegisterReceiveEndpoint`/`RegisterSendEndPoint` reordered generic parameters (`TMqOptions, TMessage, TConsumer`), removed `concurrencyLimit` parameter, removed default `AutoDelete`/`Durable` overrides on bus configuration. Source-incompatible with pre-8.x registrations — update call sites accordingly. |
