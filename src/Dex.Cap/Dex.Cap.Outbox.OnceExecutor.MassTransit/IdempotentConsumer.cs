using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Dex.Cap.Common.Ef.Interfaces;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.MassTransit.Rabbit;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit;

/// <summary>
/// Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.
/// MessageId - ключ идемпотентности.
/// Перед использованием, убедитесь что TDbContext зарегистрирован OnceExecutor
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TDbContext"></typeparam>
public abstract class IdempotentConsumer<TMessage, TDbContext> : BaseConsumer<TMessage>
    where TMessage : class
{
    private const uint DefaultTimeoutInSeconds = 60;

    protected TDbContext DbContext { get; }
    private readonly IOnceExecutor<IEfOptions, TDbContext> _onceExecutor;

    protected IdempotentConsumer([DisallowNull] TDbContext dbContext, IOnceExecutor<IEfOptions, TDbContext> onceExecutor, ILogger logger) : base(logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    protected sealed override async Task Process(ConsumeContext<TMessage> context)
    {
        if (!context.MessageId.HasValue)
        {
            throw new InvalidOperationException("Consume context must have MessageId");
        }

        await _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            async (_, _) => await IdempotentProcess(context),
            options: new EfOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew, TimeoutInSeconds = GetTimeoutInSeconds() },
            cancellationToken: context.CancellationToken
        );
    }

    protected abstract Task IdempotentProcess(ConsumeContext<TMessage> context);

    protected virtual uint GetTimeoutInSeconds() => DefaultTimeoutInSeconds;

    private static string GetIdempotentKey(ConsumeContext<TMessage> context)
    {
        if (context.MessageId == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Message is not IHaveIdempotenceKey k)
        {
            return context.MessageId.Value.ToString("N");
        }

        if (k is { IdempotentKey: null } && context.Message is IOutboxMessage o)
        {
            return o.MessageId.ToString("N");
        }

        return k.IdempotentKey ?? context.MessageId.Value.ToString("N");
    }
}
