using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxService
{
    /// <summary>
    /// Id корреляции
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Поставить outbox-сообщение в очередь на публикацию.
    /// Метод не проверяет транзакцию, только добавляет сообщение в change tracker контекста.
    /// NOTE. LockTimeout должен превышать время обработки сообщения, иначе обработка зациклится.
    /// Значение по умолчанию 30 секунд, минимум 10 секунд.
    /// </summary>
    /// <exception cref="Exceptions.OutboxContentTooLargeException">
    /// Размер сериализованного тела превышает <see cref="Options.OutboxOptions.MaxContentLength"/>.
    /// Наследует <see cref="Exceptions.OutboxException"/>.
    /// </exception>
    Task<Guid> EnqueueAsync<T>(
        T message,
        Guid? correlationId = null,
        DateTime? startAtUtc = null,
        TimeSpan? lockTimeout = null,
        CancellationToken cancellationToken = default)
        where T : class, IOutboxMessage;

    /// <summary>
    /// Проверить, существует ли уже операция с указанным correlationId.
    /// </summary>
    Task<bool> IsOperationExistsAsync(
        Guid? correlationId = null,
        CancellationToken cToken = default);
}