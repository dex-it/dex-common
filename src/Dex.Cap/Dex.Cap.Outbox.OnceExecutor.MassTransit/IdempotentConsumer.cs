using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Interfaces;
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
public abstract class IdempotentConsumer<TMessage, TDbContext>(
    IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor,
    ILogger logger) : BaseConsumer<TMessage>(logger)
    where TMessage : class
{
    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.Default;

    protected sealed override Task Process(ConsumeContext<TMessage> context)
    {
        return onceExecutor.ExecuteAsync(
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

    /// <summary>
    /// Запись об ошибке с ключом идемпотентности.
    /// </summary>
    /// <remarks>
    /// Ключ добавляется областью логирования: по нему запись связывается с записью once-executor
    /// и с повторной доставкой того же сообщения. В шаблон базового класса он не входит —
    /// идемпотентность живёт на этом слое.
    /// </remarks>
    protected override void LogError(ConsumeContext<TMessage> context, Exception e)
    {
        using var scope = Logger.BeginScope(new Dictionary<string, object?> { ["IdempotentKey"] = TryGetIdempotentKey(context) });

        base.LogError(context, e);
    }

    /// <summary>
    /// Ключ идемпотентности или null, если вычислить его нельзя.
    /// </summary>
    /// <remarks>
    /// Вычисление ключа само бросает исключение — например, у сообщения без
    /// <see cref="IIdempotentKey"/> может не быть MessageId. В обработчике ошибки это подменило бы
    /// исходное исключение, поэтому ключ просто не попадает в запись: причину несёт исключение.
    /// </remarks>
    private string? TryGetIdempotentKey(ConsumeContext<TMessage> context)
    {
        try
        {
            return GetIdempotentKey(context);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// Гарантирует только одно выполнение, в случае повтора просто выходит без ошибок.
/// MessageId - ключ идемпотентности.
/// Перед использованием, убедитесь что TDbContext зарегистрирован OnceExecutor
/// </summary>
/// <remarks>
/// Вариант для консьюмера на несколько типов сообщений: он не зависит от TMessage, но и не
/// наследует BaseConsumer, поэтому Consume, обработка исключений и запись об ошибке — за
/// наследником. Запись той же формы даёт <see cref="ConsumerLoggerExtensions"/>.LogConsumeError,
/// ключ идемпотентности добавляется к ней областью логирования.
/// </remarks>
public abstract class IdempotentConsumer<TDbContext>(IOnceExecutor<IEfTransactionOptions, TDbContext> onceExecutor)
{
    /// <summary>
    /// Переопределить EfTransactionOptions
    /// </summary>
    protected virtual EfTransactionOptions TransactionOptions => EfTransactionOptions.Default;

    /// <summary>
    /// Идемпотентное выполнение операции
    /// </summary>
    protected Task IdempotentProcess<TMessage>(
        ConsumeContext<TMessage> context,
        Func<TDbContext, CancellationToken, Task> operation)
        where TMessage : class
    {
        return onceExecutor.ExecuteAsync(
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