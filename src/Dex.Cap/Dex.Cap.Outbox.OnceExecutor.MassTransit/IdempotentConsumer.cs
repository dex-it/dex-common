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

    //todo: после обновления с net8 на net10 использовать ключевое слово field
    private EfTransactionOptions? _transactionOptions;
    private EfTransactionOptions TransactionOptions => _transactionOptions ??= TransactionOptionsInit;

    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptionsInit => EfTransactionOptions.DefaultRequiresNew;

    /// <inheritdoc/>
    protected IdempotentConsumer(
        IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor,
        ILogger logger)
        : base(logger)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
    }

    /// <inheritdoc/>
    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            async (_, _) => await IdempotentProcess(context).ConfigureAwait(false),
            options: TransactionOptions,
            cancellationToken: context.CancellationToken
        );
    }

    /// <summary>
    /// Идемпотентное выполнение операции
    /// </summary>
    protected abstract Task IdempotentProcess(ConsumeContext<TMessage> context);

    /// <summary>
    /// Вычисление ключа идемпотентности
    /// </summary>
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

    //todo: после обновления с net8 на net10 использовать ключевое слово field
    private EfTransactionOptions? _transactionOptions;
    private EfTransactionOptions TransactionOptions => _transactionOptions ??= TransactionOptionsInit;

    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptionsInit => EfTransactionOptions.DefaultRequiresNew;

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

    /// <summary>
    /// Вычисление ключа идемпотентности
    /// </summary>
    protected virtual string GetIdempotentKey<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class => context.GetIdempotentKey();
}