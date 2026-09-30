using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Interfaces;

/// <summary>
/// Постановка исходящих сообщений: точка входа аутбокса для прикладного кода.
/// </summary>
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
    /// <exception cref="Exceptions.DiscriminatorResolveException">
    /// Дискриминатор типа сообщения не найден среди загруженных сборок сервиса. В отличие от инбокса
    /// наследует <see cref="Exception"/> напрямую, а не <see cref="Exceptions.OutboxException"/>: перехват по
    /// базовому типу аутбокса его не поймает.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="lockTimeout"/> меньше 10 секунд, <paramref name="startAtUtc"/> раньше чем час назад,
    /// либо <paramref name="correlationId"/> равен <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="Exceptions.OutboxContentTooLargeException">
    /// Размер сериализованного тела превышает <see cref="Options.OutboxOptions.MaxContentLengthBytes"/>.
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