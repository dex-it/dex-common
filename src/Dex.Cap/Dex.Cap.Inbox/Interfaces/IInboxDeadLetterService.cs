using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Возврат похороненных сообщений в обработку после устранения причины отказа.
/// </summary>
/// <remarks>
/// DeadLettered это терминальный статус: сам собой в обработку он не возвращается. Когда причина отказа
/// устранена, оператор возвращает сообщение этим сервисом, а не правкой таблицы руками: возврат требует
/// согласованного сброса нескольких полей сразу, и ошибка хотя бы в одном из них похоронила бы сообщение
/// снова на первом же цикле. Затрагиваются только дискриминаторы, обработчик которых зарегистрирован в
/// этом сервисе: чужие похороненные сообщения возврату не подлежат, их разбирает их собственный потребитель.
/// </remarks>
public interface IInboxDeadLetterService
{
    /// <summary>
    /// Вернуть в обработку одно похороненное сообщение по паре MessageId + ConsumerId.
    /// </summary>
    /// <param name="identity">Идентичность похороненного сообщения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// <see langword="true"/>, если сообщение было похоронено и возвращено в обработку;
    /// <see langword="false"/>, если возвращать нечего: сообщения нет, оно не в статусе DeadLettered,
    /// или его дискриминатор не обслуживается этим сервисом.
    /// </returns>
    Task<bool> RequeueAsync(InboxMessageIdentity identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вернуть в обработку все похороненные сообщения этого сервиса.
    /// </summary>
    /// <remarks>
    /// Массовый возврат после устранения системной причины отказа. Затрагивает только свои дискриминаторы,
    /// поэтому в общей таблице чужие похороненные сообщения не задеваются.
    /// </remarks>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сколько сообщений возвращено в обработку.</returns>
    Task<int> RequeueAllAsync(CancellationToken cancellationToken = default);
}
