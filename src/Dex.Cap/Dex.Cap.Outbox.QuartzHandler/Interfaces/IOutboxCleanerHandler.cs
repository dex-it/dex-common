using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.AspNetScheduler.Interfaces
{
    internal interface IOutboxCleanerHandler
    {
        /// <exception cref="OperationCanceledException"/>
        Task Execute(TimeSpan olderThan, CancellationToken cancellationToken);
    }
}