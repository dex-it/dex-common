# Dex.Cap.Outbox

Implementation of the Outbox pattern, which allows to insert outgoing commands for asynchronous execution into the database atomically together with the main operation.
The template ensures that commands will not be lost.

* Performs the operation, publishes messages to the outbox queue as part of the transaction.
* All messages are linked by a single CorrelationId.
* Avoids closures.
* Allows you to set the necessary services in the operation state.
* Verifying the success of the operation.

### Basic usage
```csharp
await outboxService.ExecuteOperationAsync(correlation, new { Logger = logger },
    async (token, outboxContext) =>
    {
        outboxContext.State.Logger.LogDebug("DEBUG...");

        await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);
        await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "Command1" }, token);
        await outboxContext.EnqueueAsync(new TestOutboxCommand2 { Args = "Command2" }, token);
    }, CancellationToken.None);
```

# Dex.Cap.OnceExecutor

A service that guarantees idempotence: the operation will be performed once.

Automatically checks that the code has already been executed earlier.

Takes into account the need to return the value of an already performed operation.

### Basic usage
```csharp
await using (var testDbContext = new TestDbContext(DbName))
{
    var ex = new OnceExecutorEf<TestDbContext, User>(testDbContext);
    var result = await ex.Execute(stepId,
        (context, c) => context.Users.AddAsync(user, c).AsTask(),
        (context, c) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", cancellationToken: c)
    );
}
```