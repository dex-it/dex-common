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
    where TMessage : IOutboxMessage
{
    private readonly IOnceExecutor<IEfOptions, TDbContext> _onceExecutor;

    protected IdempotentOutboxHandler(IOnceExecutor<IEfOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    /// <inheritdoc />
    public abstract Task ProcessMessage(TMessage message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task ProcessMessage(IOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        if (outboxMessage is not TMessage typedMessage)
        {
            throw new InvalidOperationException($"Unable cast IOutboxMessage to type: {typeof(TMessage)}");
        }

        await _onceExecutor.ExecuteAsync(
            outboxMessage.MessageId.ToString("N"),
            async (_, token) => await ProcessMessage(typedMessage, token),
            options: new EfOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew },
            cancellationToken: cancellationToken
        );
    }
}