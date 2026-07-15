using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Interfaces;

public interface IInboxService
{
    /// <summary>
    /// Принять входящее сообщение и сохранить его для последующей фоновой обработки.
    /// </summary>
    /// <remarks>
    /// В отличие от <c>IOutboxService.EnqueueAsync</c> метод сохраняет сообщение немедленно, своей транзакцией,
    /// и не участвует в транзакции вызывающего кода: смысл инбокса в том, чтобы зафиксировать сообщение
    /// ДО подтверждения источнику, а бизнес-работа выполняется позже фоновым обработчиком.
    /// <para>
    /// Повторная доставка того же сообщения возвращает <see cref="InboxEnqueueStatus.Duplicate"/> без исключения:
    /// источнику в этом случае следует подтвердить сообщение, а не отправлять его в очередь ошибок.
    /// </para>
    /// NOTE. lockTimeout должен превышать время обработки сообщения, иначе аренда истечёт
    /// и сообщение будет обработано повторно. Значение по умолчанию 30 сек, минимальное 10 сек.
    /// </remarks>
    Task<InboxEnqueueStatus> EnqueueAsync<T>(
        T message,
        InboxMessageIdentity identity,
        TimeSpan? lockTimeout = null,
        CancellationToken cancellationToken = default)
        where T : class, IInboxMessage;
}
