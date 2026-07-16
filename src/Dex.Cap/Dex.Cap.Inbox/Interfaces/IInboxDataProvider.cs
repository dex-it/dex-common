using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Jobs;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxDataProvider
{
    /// <summary>
    /// Сохранить принятое сообщение. Дубль по паре MessageId + ConsumerId не сохраняется повторно.
    /// </summary>
    Task<InboxEnqueueStatus> Add(InboxEnvelope inboxEnvelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Захватить партию сообщений, готовых к обработке, взяв на них аренду.
    /// </summary>
    Task<IInboxLockedJob[]> GetWaitingJobs(CancellationToken cancellationToken = default);

    Task JobFail(IInboxLockedJob inboxJob, string? errorMessage = null, Exception? exception = null,
        CancellationToken cancellationToken = default);

    Task JobSucceed(IInboxLockedJob inboxJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Количество сообщений, ожидающих обработки ЭТИМ сервисом. Используется как метрика глубины инбокса.
    /// </summary>
    /// <remarks>
    /// Считает только дискриминаторы с зарегистрированным обработчиком: сообщения чужих потребителей
    /// этот сервис никогда не заберёт, и включать их в свою глубину очереди означало бы залипший алерт.
    /// </remarks>
    int GetFreeMessagesCount();

    /// <summary>
    /// Количество похороненных сообщений, ожидающих ручного разбора.
    /// </summary>
    /// <remarks>Чистка их не удаляет, поэтому объём нужно наблюдать отдельно.</remarks>
    int GetDeadLetteredMessagesCount();

    /// <summary>
    /// Вернуть в обработку одно похороненное сообщение по паре MessageId + ConsumerId.
    /// </summary>
    /// <returns>Число возвращённых строк: ноль или один.</returns>
    Task<int> RequeueDeadLetteredAsync(InboxMessageIdentity identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вернуть в обработку все похороненные сообщения этого сервиса.
    /// </summary>
    /// <returns>Число возвращённых строк.</returns>
    Task<int> RequeueAllDeadLetteredAsync(CancellationToken cancellationToken = default);
}
