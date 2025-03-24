using System.Transactions;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Идемпотентный обработчик сообщений аутбокса
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TDbContext"></typeparam>
public abstract class IdempotentOutboxHandler<TMessage, TDbContext> : IOutboxMessageHandler<TMessage>
    where TMessage : class
{
    private readonly IOnceExecutor<IEfOptions, TDbContext> _onceExecutor;

    protected IdempotentOutboxHandler(IOnceExecutor<IEfOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    protected abstract Task IdempotentProcess(TMessage message, CancellationToken cancellationToken);

    protected virtual string GetIdempotentKey(TMessage message) => GetIdempotentKeyInner(message);

    public async Task Process(TMessage outboxMessage, CancellationToken cancellationToken)
    {
        await _onceExecutor.ExecuteAsync(
            GetIdempotentKey(outboxMessage),
            async (_, token) => await IdempotentProcess(outboxMessage, token),
            options: new EfOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew },
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