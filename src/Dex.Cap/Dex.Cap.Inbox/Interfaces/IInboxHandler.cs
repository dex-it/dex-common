using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.Interfaces;

public interface IInboxHandler
{
    /// <summary>
    /// Обработать партию принятых сообщений.
    /// </summary>
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
