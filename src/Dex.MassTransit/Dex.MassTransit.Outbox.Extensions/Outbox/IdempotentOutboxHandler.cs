using System.Transactions;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Microsoft.EntityFrameworkCore;

namespace Dex.MassTransit.Outbox.Extensions.Outbox;

/// <summary>
/// Идемпотентный обработчик сообщений аутбокса
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class IdempotentOutboxHandler<TMessage> : IOutboxMessageHandler<TMessage>
    where TMessage : BaseOutboxMessage
{
    private readonly IOnceExecutor<IEfOptions, DbContext> _onceExecutor;

    protected IdempotentOutboxHandler(IOnceExecutor<IEfOptions, DbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    /// <inheritdoc />
    public abstract Task ProcessMessage(TMessage message, CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
    {
        if (outbox is not BaseOutboxMessage baseMessage)
        {
            throw new InvalidOperationException("Message type must be inherited from BaseOutboxMessage");
        }

        if (outbox is not TMessage typedMessage)
        {
            throw new InvalidOperationException($"Unable cast IOutboxMessage to type: {typeof(TMessage)}");
        }

        await _onceExecutor.ExecuteAsync(
            baseMessage.MessageId.ToString("N"),
            async (_, token) => await ProcessMessage(typedMessage, token),
            options: new EfOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew },
            cancellationToken: cancellationToken
        );
    }
}