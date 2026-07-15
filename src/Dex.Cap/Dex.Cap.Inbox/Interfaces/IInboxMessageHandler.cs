using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Обработчик входящего сообщения.
/// </summary>
/// <remarks>
/// Вызывается внутри транзакции, общей с фиксацией статуса сообщения: изменения, сделанные обработчиком
/// через тот же DbContext, коммитятся атомарно с переводом сообщения в Succeeded. Поэтому обработчик
/// не должен коммитить транзакцию сам.
/// <para>
/// Транзакция БД не распространяется на внешние вызовы (HTTP, брокер, файлы): они выполнятся повторно
/// при повторной обработке, поэтому должны быть идемпотентны сами по себе.
/// </para>
/// </remarks>
public interface IInboxMessageHandler<in TMessage>
    where TMessage : IInboxMessage
{
    Task Process(TMessage message, CancellationToken cancellationToken = default);
}
