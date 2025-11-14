using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Идемпотентный обработчик сообщений аутбокса
/// </summary>
public abstract class IdempotentOutboxHandler<TMessage, TDbContext>(IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor)
    : IOutboxMessageHandler<TMessage> where TMessage : class, IIdempotentKey, IOutboxMessage
{
    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.DefaultRequiresNew;

    protected abstract Task IdempotentProcess(TMessage message, CancellationToken cancellationToken);

    protected virtual string GetIdempotentKey(TMessage message) => GetIdempotentKeyInner(message);

    public Task Process(TMessage outboxMessage, CancellationToken cancellationToken)
    {
        return onceExecutor.ExecuteAsync(
            GetIdempotentKey(outboxMessage),
            async (_, token) => await IdempotentProcess(outboxMessage, token).ConfigureAwait(false),
            options: TransactionOptions,
            cancellationToken: cancellationToken
        );
    }

    private static string GetIdempotentKeyInner<T>(T message) where T : class
        => message is IIdempotentKey key
            ? key.IdempotentKey
            : throw new ArgumentNullException(nameof(message));
}