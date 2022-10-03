# Dex.Events.Distributed

Event Management Service: allows to generate parameterized events and notify any other service about them.

* Events and event-handlers can be located in any part of the system.
* Implemented the ability to subscribe to events from different services, as well as have different handlers in different services.
* MassTransit-based implementation.
* A consumer is created under the hood, tied to the type of message, inside which a handler of this type is taken from the DI-container and a handler is called from it.

### Basic usage
```csharp
await using var serviceProvider = InitServiceCollection()
    .RegisterDistributedEventRaiser()
    .Configure<RabbitMqOptions>(_ => { })
    .AddMassTransit(c =>
    {
        c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler, TestOnCardAddedHandler2>();
        c.RegisterBus((context, configurator) =>
        {
            context.RegisterDistributedEventSendEndPoint<OnCardAdded>();
            context.RegisterDistributedEventReceiveEndpoint<OnCardAdded>(configurator);
        });
    })
    .BuildServiceProvider();

var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

await dbContext.Users.AddAsync(entity, CancellationToken.None);
await dbContext.SaveChangesAsync(CancellationToken.None);
await eventRaiser.RaiseAsync(new OnCardAdded { CardId = Guid.NewGuid() }, CancellationToken.None);
```

# Dex.Events.Distributed.OutboxExtensions

An extension that allows to use the outbox when working with distributed events.

All operations performed publish messages to the outbox queue as part of the transaction.

### Basic usage
```csharp
await using var serviceProvider = InitServiceCollection()
    .RegisterOutboxDistributedEventHandler()
    .Configure<RabbitMqOptions>(_ => { })
    .AddMassTransit(c =>
    {
        c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler>();
        c.RegisterBus((context, configurator) =>
        {
            context.RegisterDistributedEventSendEndPoint<OnCardAdded>();
            context.RegisterDistributedEventReceiveEndpoint<OnCardAdded>(configurator);
        });
    })
    .BuildServiceProvider();

var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
var logger = serviceProvider.GetService<ILogger<OutboxTests>>();

await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Logger = logger },
    async (token, outboxContext) =>
    {
        outboxContext.State.Logger.LogDebug("DEBUG...");
        await outboxContext.DbContext.Users.AddAsync(entity, token);
        await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
        await outboxContext.RaiseDistributedEventAsync(new OnCardAdded { CardId = Guid.NewGuid() }, token);
    },
    CancellationToken.None);
```