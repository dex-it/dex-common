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
    private readonly TDbContext _dbContext;

    protected TransactionalConsumer(TDbContext context, ILogger logger)
        : base(logger)
    {
        _dbContext = context;
    }

    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.DefaultRequiresNew;

    protected abstract Task ProcessInTransaction(ConsumeContext<TMessage> context);

    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        return _dbContext.ExecuteInTransactionScopeAsync(
            context,
            async (state, _) => await ProcessInTransaction(state).ConfigureAwait(false),
            async (state, _) => await VerifySucceeded(state).ConfigureAwait(false),
            TransactionOptions,
            context.CancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded(ConsumeContext<TMessage> context)
    {
        return Task.FromResult(false);
    }
}

/// <summary>
/// Выполнение операции в транзакции
/// </summary>
public abstract class TransactionalConsumer<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    protected TransactionalConsumer(TDbContext context)
    {
        _dbContext = context;
    }

    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.DefaultRequiresNew;

    protected Task ProcessInTransaction<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation)
        where TMessage : class
    {
        return _dbContext.ExecuteInTransactionScopeAsync(
            (ConsumeContext: context, DbContext: _dbContext),
            async (state, token) => await operation.Invoke(state.DbContext, token),
            async (state, _) => await VerifySucceeded(state.ConsumeContext).ConfigureAwait(false),
            TransactionOptions,
            context.CancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        return Task.FromResult(false);
    }
}