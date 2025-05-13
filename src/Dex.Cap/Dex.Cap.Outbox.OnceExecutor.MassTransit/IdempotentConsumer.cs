using System.Transactions;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions;
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

    private readonly IOnceExecutor<IEfOptions, TDbContext> _onceExecutor;

    protected IdempotentConsumer(
        IOnceExecutor<IEfOptions, TDbContext> onceExecutor,
        ILogger logger)
        : base(logger)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            async (_, _) => await IdempotentProcess(context).ConfigureAwait(false),
            options: new EfOptions
            {
                TransactionScopeOption = TransactionScopeOption.RequiresNew, TimeoutInSeconds = GetTimeoutInSeconds()
            },
            cancellationToken: context.CancellationToken
        );
    }

    protected abstract Task IdempotentProcess(ConsumeContext<TMessage> context);

    protected virtual uint GetTimeoutInSeconds() => DefaultTimeoutInSeconds;

    protected virtual string GetIdempotentKey(ConsumeContext<TMessage> context) => context.GetIdempotentKey();
}

/// <summary>
/// Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.
/// MessageId - ключ идемпотентности.
/// Перед использованием, убедитесь что TDbContext зарегистрирован OnceExecutor
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
public abstract class IdempotentConsumer<TDbContext>
{
    private const uint DefaultTimeoutInSeconds = 60;

    private readonly IOnceExecutor<IEfOptions, TDbContext> _onceExecutor;

    /// <summary>
    /// Конструктор
    /// </summary>
    protected IdempotentConsumer(IOnceExecutor<IEfOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor;
    }

    /// <summary>
    /// Идемпотентное выполнение операции
    /// </summary>
    protected Task IdempotentProcess<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation,
        IEfOptions? options = default)
        where TMessage : class
    {
        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            operation,
            options: options ?? new EfOptions
            {
                TransactionScopeOption = TransactionScopeOption.RequiresNew, TimeoutInSeconds = GetTimeoutInSeconds()
            },
            cancellationToken: context.CancellationToken
        );
    }

    protected virtual uint GetTimeoutInSeconds() => DefaultTimeoutInSeconds;

    protected virtual string GetIdempotentKey<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class => context.GetIdempotentKey();
}