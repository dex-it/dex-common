using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Идемпотентный обработчик сообщений аутбокса
/// </summary>
public abstract class IdempotentOutboxHandler<TMessage, TDbContext> : IOutboxMessageHandler<TMessage>
    where TMessage : class, IIdempotentKey
{
    private readonly IOnceExecutor<IEfTransactionOptions, TDbContext> _onceExecutor;

    protected IdempotentOutboxHandler(IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    protected virtual EfTransactionOptions TransactionOptions { private get; init; } =
        EfTransactionOptions.DefaultRequiresNew;

    protected abstract Task IdempotentProcess(TMessage message, CancellationToken cancellationToken);

    protected virtual string GetIdempotentKey(TMessage message) => GetIdempotentKeyInner(message);

    public Task Process(TMessage outboxMessage, CancellationToken cancellationToken)
    {
        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(outboxMessage),
            async (_, token) => await IdempotentProcess(outboxMessage, token).ConfigureAwait(false),
            options: TransactionOptions,
            cancellationToken: cancellationToken
        );
    }

    private static string GetIdempotentKeyInner<T>(T message)
        where T : class
    {
        if (message is not IIdempotentKey key)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return key.IdempotentKey;
    }
}