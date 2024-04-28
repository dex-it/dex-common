# Dex.Cap.Outbox

Implementation of the Outbox pattern, which allows to insert outgoing commands for asynchronous execution into the database atomically together with the main operation.
The template ensures that commands will not be lost.

* Performs the operation, publishes messages to the outbox queue as part of the transaction.
* All messages are linked by a single CorrelationId.
* Avoids closures.
* Allows you to set the necessary services in the operation state.
* Verifying the success of the operation.
* Discriminate message type, for serialization

### Basic usage
```csharp
await outboxService.ExecuteOperationAsync(
    correlationId,
    state: new { Logger = logger },
    async (token, outboxContext) =>
    {
        outboxContext.State.Logger.LogDebug("DEBUG...");

        await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);
        await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "Command1" }, token);
        await outboxContext.EnqueueAsync(new TestOutboxCommand2 { Args = "Command2" }, token);
    },
    CancellationToken.None);
```

# Dex.Cap.OnceExecutor

A service that guarantees idempotence: the operation will be performed once.

Automatically checks that the code has already been executed earlier.

Takes into account the need to return the value of an already performed operation.

The basic usage is based on the idempotency key.

### Basic usage
```csharp
var serviceProvider = InitServiceCollection().BuildServiceProvider();
var executor = serviceProvider.GetRequiredService<IOnceExecutor<TestDbContext>>();
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