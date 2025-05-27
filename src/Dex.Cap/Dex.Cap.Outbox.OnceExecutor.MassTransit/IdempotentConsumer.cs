using System.Transactions;
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
    private readonly EfTransactionOptions _transactionOptions;
    private readonly IOnceExecutor<IEfTransactionOptions, TDbContext> _onceExecutor;

    protected IdempotentConsumer(
        IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor,
        ILogger logger)
        : base(logger)
    {
        _onceExecutor = onceExecutor ?? throw new ArgumentNullException(nameof(onceExecutor));
        _transactionOptions = new EfTransactionOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew };
    }

    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        _transactionOptions.IsolationLevel = GetIsolationLevel();
        _transactionOptions.TimeoutInSeconds = GetTimeoutInSeconds();
        _transactionOptions.ClearChangeTrackerOnRetry = ClearChangeTrackerOnRetry();

        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            async (_, _) => await IdempotentProcess(context).ConfigureAwait(false),
            options: _transactionOptions,
            cancellationToken: context.CancellationToken
        );
    }

    protected abstract Task IdempotentProcess(ConsumeContext<TMessage> context);

    protected virtual IsolationLevel GetIsolationLevel() => _transactionOptions.IsolationLevel;

    protected virtual uint GetTimeoutInSeconds() => _transactionOptions.TimeoutInSeconds;

    protected virtual bool ClearChangeTrackerOnRetry() => _transactionOptions.ClearChangeTrackerOnRetry;

    protected virtual string GetIdempotentKey(ConsumeContext<TMessage> context) => context.GetIdempotentKey();
}

/// <summary>
/// Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.
/// MessageId - ключ идемпотентности.
/// Перед использованием, убедитесь что TDbContext зарегистрирован OnceExecutor
/// </summary>
public abstract class IdempotentConsumer<TDbContext>
{
    private readonly EfTransactionOptions _transactionOptions;
    private readonly IOnceExecutor<IEfTransactionOptions, TDbContext> _onceExecutor;

    /// <summary>
    /// Конструктор
    /// </summary>
    protected IdempotentConsumer(IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor)
    {
        _onceExecutor = onceExecutor;
        _transactionOptions = new EfTransactionOptions { TransactionScopeOption = TransactionScopeOption.RequiresNew };
    }

    /// <summary>
    /// Идемпотентное выполнение операции
    /// </summary>
    protected Task IdempotentProcess<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation)
        where TMessage : class
    {
        _transactionOptions.IsolationLevel = GetIsolationLevel();
        _transactionOptions.TimeoutInSeconds = GetTimeoutInSeconds();
        _transactionOptions.ClearChangeTrackerOnRetry = ClearChangeTrackerOnRetry();

        return _onceExecutor.ExecuteAsync(
            GetIdempotentKey(context),
            operation,
            options: _transactionOptions,
            cancellationToken: context.CancellationToken
        );
    }

    protected virtual IsolationLevel GetIsolationLevel() => _transactionOptions.IsolationLevel;

    protected virtual uint GetTimeoutInSeconds() => _transactionOptions.TimeoutInSeconds;

    protected virtual bool ClearChangeTrackerOnRetry() => _transactionOptions.ClearChangeTrackerOnRetry;

    protected virtual string GetIdempotentKey<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class => context.GetIdempotentKey();
}