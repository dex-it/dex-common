using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Выполнение операции в транзакции
/// </summary>
public abstract class TransactionalOutboxHandler<TMessage, TDbContext>(TDbContext context) : IOutboxMessageHandler<TMessage>
    where TMessage : class, IOutboxMessage, new()
    where TDbContext : DbContext
{
    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.DefaultRequiresNew;

    protected abstract Task ProcessInTransaction(TMessage message, CancellationToken cancellationToken);

    public Task Process(TMessage message, CancellationToken cancellationToken)
    {
        return context.ExecuteInTransactionScopeAsync(
            message,
            async (state, token) => await ProcessInTransaction(state, token).ConfigureAwait(false),
            async (state, token) => await VerifySucceeded(state, token).ConfigureAwait(false),
            TransactionOptions,
            cancellationToken);
    }

    protected virtual Task<bool> VerifySucceeded(TMessage message, CancellationToken cancellationToken) => Task.FromResult(false);
}