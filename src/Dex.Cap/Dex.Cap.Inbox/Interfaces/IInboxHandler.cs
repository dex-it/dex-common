using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Разбор инбокса: один цикл захвата и обработки принятых сообщений.
/// </summary>
public interface IInboxHandler
{
    /// <summary>
    /// Обработать партию принятых сообщений.
    /// </summary>
    /// <returns>
    /// Количество захваченных на обработку сообщений. Позволяет вызывающему коду отличить
    /// пустую очередь от полной партии и не делать паузу, пока есть что обрабатывать.
    /// </returns>
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}