using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.AspNetScheduler.Interfaces;

internal interface IInboxCleanerHandler
{
    Task Execute(TimeSpan olderThan, CancellationToken cancellationToken);
}
