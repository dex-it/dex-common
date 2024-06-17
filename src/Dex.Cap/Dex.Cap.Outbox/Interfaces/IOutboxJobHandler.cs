using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Jobs;

namespace Dex.Cap.Outbox.Interfaces;

internal interface IOutboxJobHandler
{
    /// <exception cref="OperationCanceledException"/>
    Task ProcessJob(IOutboxLockedJob job, CancellationToken cancellationToken);
}