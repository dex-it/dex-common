using Dex.Cap.Common.Ef;
using Dex.Cap.OnceExecutor;
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
public abstract class IdempotentConsumer<TMessage, TDbContext> : BaseConsumer<TMessage>
    where TMessage : class
{
    private readonly IOnceExecutor<IEfTransactionOptions, TDbContext> _onceExecutor;

    protected virtual EfTransactionOptions TransactionOptions { private get; init; } =
        EfTransactionOptions.DefaultRequiresNew;

    protected IdempotentConsumer(
        IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor,
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
            options: TransactionOptions,
            cancellationToken: context.CancellationToken
        );
    }

    protected abstract Task IdempotentProcess(ConsumeContext<TMessage> context);

    protected virtual string GetIdempotentKey(ConsumeContext<TMessage> context) => context.GetIdempotentKey();
}

/// <summary>
/// Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.
/// MessageId - ключ идемпотентности.
/// Перед использованием, убедитесь что TDbContext зарегистрирован OnceExecutor
/// </summary>
public abstract class IdempotentConsumer<TDbContext>
{
    private readonly IOnceExecutor<IEfTransactionOptions, TDbContext> _onceExecutor;

    /// <summary>
    /// Конструктор
    /// </summary>
    protected IdempotentConsumer(IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor;
    }

    protected virtual EfTransactionOptions TransactionOptions { private get; init; } =
        EfTransactionOptions.DefaultRequiresNew;

    /// <summary>
    /// Идемпотентное выполнение операции
    /// </summary>
    protected Task IdempotentProcess<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation)
        where TMessage : class
    {
        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            operation,
            options: TransactionOptions,
            cancellationToken: context.CancellationToken
        );
    }

    protected virtual string GetIdempotentKey<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class => context.GetIdempotentKey();
}