using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Scheduler.Interfaces
{
    internal interface IOutboxCleanerHandler
    {
        /// <exception cref="OperationCanceledException"/>
        Task ExecuteAsync(TimeSpan olderThan, CancellationToken cancellationToken);
    }
}