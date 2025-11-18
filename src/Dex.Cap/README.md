# Dex.Cap.Outbox

Implementation of the Outbox pattern, which allows to insert outgoing commands for asynchronous execution into the database atomically together with the main operation.
The template ensures that commands will not be lost.

* Performs the operation, publishes messages to the outbox queue as part of the transaction.
* All messages are linked by a single CorrelationId.
* Avoids closures.
* Allows you to set the necessary services in the operation state.
* Verifying the success of the operation.
* Discriminate message type, for serialization

#### Registration
```csharp
// implement OutboxMessage
public class OutboxMessage : IOutboxMessage
{
    public static string OutboxTypeId => "3961131e-3961-4c38-8a30-09b91cb56d60";

    public string Args { get; init; }
}

// implement specific OutboxMessageHandler (if not, the OutboxMessage will be auto-published)
public class OutboxMessageHandler : IOutboxMessageHandler<OutboxMessage>
{
    private readonly ILogger<OutboxMessageHandler> _logger;

    public OutboxMessageHandler(ILogger<OutboxMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task Process(OutboxMessage message, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        _logger.LogInformation("Processed message at {Now}, Args: {MessageArgs}", DateTime.Now, message.Args);
    }
}

// ConfigureServices
services.AddOutbox<DbContext>();
services.RegisterOutboxScheduler(periodSeconds: 1, cleanupDays: 90); // register scheduler
services.AddScoped<IOutboxMessageHandler<OutboxMessage>, OutboxMessageHandler>(); // defining a specific handler
services.AddOutboxPublisher(); // auto-publish any outbox message when a specific handler is not defined

// OnModelCreating
modelBuilder.OutboxModelCreating();

// remarks: when using optimistic concurrency in PostgreSQL - it is necessary to exclude type OutboxEnvelope
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(OutboxEnvelope));
```

#### Basic usage

```csharp
public class SomeService(IOutboxService outbox, AppDbContext dbContext)
{
    public async Task Example()
    {
        await dbContext.Users.AddAsync(new User { Name = name }, token);

        await outbox.EnqueueAsync(new OutboxMessage { Args = "custom args" }, token);

        await dbContext.SaveChangesAsync(token);
    }
}
```

### IOutboxMessage
Outbox messages must implement the IOutboxMessage interface.

#### OutboxTypeId
Required, the string type.

A unique identifier of the message type.

#### AllowAutoPublishing
Optional, bool type, true by default.

Allows the message to be published automatically by ```services.AddOutboxPublisher();```

If set to false, it will require explicit implementation of a separate handler  ```services.AddScoped<IOutboxMessageHandler<SomeOutboxMessage>, SomeOutboxMessageHandler>();```


# Dex.Cap.OnceExecutor

A service that guarantees idempotence: the operation will be performed once.

Automatically checks that the code has already been executed earlier.

Takes into account the need to return the value of an already performed operation.

The basic usage is based on the idempotency key.

#### Registration
```csharp
// ConfigureServices
services.AddOnceExecutor<DbContext>();

// OnModelCreating
modelBuilder.OnceExecutorModelCreating();

// remarks: when using optimistic concurrency in PostgreSQL - it is necessary to exclude type LastTransaction
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(LastTransaction));
```

#### Basic usage
```csharp
var serviceProvider = InitServiceCollection().BuildServiceProvider();
var executor = serviceProvider.GetRequiredService<IOnceExecutor<DbContext>>();
var result = await executor.ExecuteAsync(
    idempotentKey,
    (dbContext, token) => dbContext.Users.AddAsync(user, token).AsTask(),
    (dbContext, token) => dbContext.Users.FirstOrDefaultAsync(x => x.Name == "Name", token));
```

### Extended usage OnceExecutor based on strategies.

The strategy implements methods:
* checking that the operation was completed successfully and not repeating it again
* modifier method
* reading method that allows to return the resulting data

It is possible to set the transaction isolation level (by default, it is set to ReadCommitted).

#### Strategy implementation
```csharp
public class Concrete1ExecutionStrategy : IOnceExecutionStrategy<Concrete1ExecutionStrategyRequest, string>
{
    private readonly TestDbContext _dbContext;
    public IsolationLevel TransactionIsolationLevel => IsolationLevel.RepeatableRead;

    public Concrete1ExecutionStrategy(TestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsAlreadyExecutedAsync(Concrete1ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
        return userDb != null;
    }

    public async Task ExecuteAsync(Concrete1ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(new TestUser { Name = argument.Value, Years = 18 }, cancellationToken);
    }

    public async Task<string?> ReadAsync(Concrete1ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
        return userDb?.Name;
    }
}
```

#### Using the strategy
```csharp
var serviceProvider = InitServiceCollection()
    .AddStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string, Concrete1ExecutionStrategy, TestDbContext>()
    .BuildServiceProvider();

var request = new Concrete1ExecutionStrategyRequest { Value = "StrategyOnceExecuteTest1" };
var executor = serviceProvider.GetRequiredService<IStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string>>();

var result = await executor.ExecuteAsync(request, CancellationToken.None);
```

### Breaking Changes
Dex.Cap.Outbox.Ef - 1.9.x: Add discriminator. Before use, you must implement type discriminator logic (IOutboxTypeDiscriminator).