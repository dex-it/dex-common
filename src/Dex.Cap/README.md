# Dex.Cap.Outbox

Implementation of the Outbox pattern, which allows to insert outgoing commands for asynchronous execution into the database atomically together with the main operation.
The template ensures that commands will not be lost.

* Performs the operation, publishes messages to the outbox queue as part of the transaction.
* All messages are linked by a single CorrelationId.
* Avoids closures.
* Allows you to set the necessary services in the operation state.
* Verifying the success of the operation.
* Discriminate message type, for serialization

### Registration
```csharp
// implement discriminator
internal class OutboxTypeDiscriminator : BaseOutboxTypeDiscriminator
{
    public OutboxTypeDiscriminator()
    {
        Add<OutboxMessage>("15CAD1F5-4C0D-4816-B5D1-E2340144C4AA");
    }
}

// implement OutboxMessage
public class OutboxMessage : IOutboxMessage
{
    public string Args { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();
}

// implement specific OutboxMessageHandler (if not, the OutboxMessage will be auto-published)
public class OutboxMessageHandler : IOutboxMessageHandler<OutboxMessage>
{
    private readonly ILogger<OutboxMessageHandler> _logger;

    public OutboxMessageHandler(ILogger<OutboxMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        _logger.LogInformation("Processed message at {Now}, Args: {MessageArgs}", DateTime.Now, message.Args);
    }

    public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
    {
        return ProcessMessage((OutboxMessage) outbox, cancellationToken);
    }
}

// ConfigureServices
serviceCollection.AddOutbox<DbContext, OutboxTypeDiscriminator>();
serviceCollection.RegisterOutboxScheduler(periodSeconds: 1, cleanupDays: 90);
serviceCollection.AddScoped<IOutboxMessageHandler<OutboxMessage>, OutboxMessageHandler>(); // defining a specific handler
serviceCollection.AddScoped(typeof(IOutboxMessageHandler<>), typeof(PublisherOutboxHandler<>)); // auto-publish any outbox message when a specific handler is not defined

// OnModelCreating
modelBuilder.OutboxModelCreating();
// remarks: when using optimistic concurrency in PostgreSQL - it is necessary to exclude type OutboxEnvelope
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(OutboxEnvelope));
```

### Basic usage
```csharp
// ctor
private readonly IOutboxService<DbContext> _outboxService;

// process
var correlationId = Guid.NewGuid();
await _outboxService.ExecuteOperationAsync(
    correlationId,
    state: new { Logger = logger },
    async (token, outboxContext) =>
    {
        outboxContext.State.Logger.LogDebug("DEBUG...");

        await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);
        await outboxContext.EnqueueAsync(new OutboxMessage { Args = "message1" }, token);
    },
    cancellationToken);
```

# Dex.Cap.OnceExecutor

A service that guarantees idempotence: the operation will be performed once.

Automatically checks that the code has already been executed earlier.

Takes into account the need to return the value of an already performed operation.

The basic usage is based on the idempotency key.

### Registration
```csharp
// ConfigureServices
serviceCollection.AddOnceExecutor<DbContext>();

// OnModelCreating
modelBuilder.OnceExecutorModelCreating();
// remarks: when using optimistic concurrency in PostgreSQL - it is necessary to exclude type LastTransaction
modelBuilder.UseXminAsConcurrencyToken(ignoreTypes: typeof(LastTransaction));
```

### Basic usage
```csharp
var serviceProvider = InitServiceCollection().BuildServiceProvider();
var executor = serviceProvider.GetRequiredService<IOnceExecutor<DbContext>>();
var result = await executor.ExecuteAsync(
    idempotentKey,
    (dbContext, token) => dbContext.Users.AddAsync(user, token).AsTask(),
    (dbContext, token) => dbContext.Users.FirstOrDefaultAsync(x => x.Name == "Name", token),
    IsolationLevel.ReadCommitted,
    CancellationToken.None
);
```

### Extended usage OnceExecutor based on strategies.

The strategy implements methods:
* checking that the operation was completed successfully and not repeating it again
* modifier method
* reading method that allows to return the resulting data

It is possible to set the transaction isolation level (by default, it is set to ReadCommitted).

### Strategy implementation
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

### Using the strategy
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