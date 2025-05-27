using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.MassTransit.Rabbit;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Выполнение операции в транзакции
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TDbContext"></typeparam>
public abstract class TransactionalConsumer<TMessage, TDbContext> : BaseConsumer<TMessage>
    where TMessage : class
    where TDbContext : DbContext
{
    private readonly EfTransactionOptions _transactionOptions;
    private readonly TDbContext _dbContext;

    protected TransactionalConsumer(TDbContext context, ILogger logger)
        : base(logger)
    {
        _dbContext = context;
        _transactionOptions = new EfTransactionOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew };
    }

    protected abstract Task ProcessInTransaction(ConsumeContext<TMessage> context);

    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        return _dbContext.ExecuteInTransactionScopeAsync(
            context,
            async (state, _) => await ProcessInTransaction(state).ConfigureAwait(false),
            async (state, _) => await VerifySucceeded(state).ConfigureAwait(false),
            _transactionOptions.TransactionScopeOption,
            GetIsolationLevel(),
            GetTimeoutInSeconds(),
            ClearChangeTrackerOnRetry(),
            context.CancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded(ConsumeContext<TMessage> context)
    {
        return Task.FromResult(false);
    }

    protected virtual IsolationLevel GetIsolationLevel() => _transactionOptions.IsolationLevel;

    protected virtual uint GetTimeoutInSeconds() => _transactionOptions.TimeoutInSeconds;

    protected virtual bool ClearChangeTrackerOnRetry() => _transactionOptions.ClearChangeTrackerOnRetry;
}

/// <summary>
/// Выполнение операции в транзакции
/// </summary>
public abstract class TransactionalConsumer<TDbContext>
    where TDbContext : DbContext
{
    private readonly EfTransactionOptions _transactionOptions;
    private readonly TDbContext _dbContext;

    protected TransactionalConsumer(TDbContext context)
    {
        _dbContext = context;
        _transactionOptions = new EfTransactionOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew };
    }

    protected Task ProcessInTransaction<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation)
        where TMessage : class
    {
        return _dbContext.ExecuteInTransactionScopeAsync(
            state: (ConsumeContext: context, DbContext: _dbContext),
            operation: async (state, token) => await operation.Invoke(state.DbContext, token),
            verifySucceeded: async (state, _) => await VerifySucceeded(state.ConsumeContext).ConfigureAwait(false),
            transactionScopeOption: _transactionOptions.TransactionScopeOption,
            isolationLevel: GetIsolationLevel(),
            timeoutInSeconds: GetTimeoutInSeconds(),
            clearChangeTrackerOnRetry: ClearChangeTrackerOnRetry(),
            cancellationToken: context.CancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        return Task.FromResult(false);
    }

    protected virtual IsolationLevel GetIsolationLevel() => _transactionOptions.IsolationLevel;

    protected virtual uint GetTimeoutInSeconds() => _transactionOptions.TimeoutInSeconds;

    protected virtual bool ClearChangeTrackerOnRetry() => _transactionOptions.ClearChangeTrackerOnRetry;
}