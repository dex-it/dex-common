using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Выполнение операции в транзакции
/// </summary>
public abstract class TransactionalOutboxHandler<TMessage, TDbContext> : IOutboxMessageHandler<TMessage>
    where TMessage : class
    where TDbContext : DbContext
{
    private readonly EfTransactionOptions _transactionOptions;
    private readonly TDbContext _dbContext;

    protected TransactionalOutboxHandler(TDbContext context)
    {
        _dbContext = context;
        _transactionOptions = new EfTransactionOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew };
    }

    protected abstract Task ProcessInTransaction(TMessage message, CancellationToken cancellationToken);

    public Task Process(TMessage message, CancellationToken cancellationToken)
    {
        return _dbContext.ExecuteInTransactionScopeAsync(
            message,
            async (state, token) => await ProcessInTransaction(state, token).ConfigureAwait(false),
            async (state, token) => await VerifySucceeded(state, token).ConfigureAwait(false),
            _transactionOptions.TransactionScopeOption,
            GetIsolationLevel(),
            GetTimeoutInSeconds(),
            cancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded(TMessage message, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    protected virtual uint GetTimeoutInSeconds() => _transactionOptions.TimeoutInSeconds;

    protected virtual IsolationLevel GetIsolationLevel() => _transactionOptions.IsolationLevel;
}