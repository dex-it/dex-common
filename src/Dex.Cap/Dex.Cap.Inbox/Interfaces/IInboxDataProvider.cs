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
    /// Количество сообщений, ожидающих обработки. Используется как метрика глубины инбокса.
    /// </summary>
    int GetFreeMessagesCount();
}
