using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Jobs;

namespace Dex.Cap.Inbox.Interfaces;

internal interface IInboxJobHandler
{
    /// <summary>
    /// Обработать одно захваченное сообщение и зафиксировать исход.
    /// </summary>
    Task ProcessJob(IInboxLockedJob job, CancellationToken cancellationToken = default);
}
