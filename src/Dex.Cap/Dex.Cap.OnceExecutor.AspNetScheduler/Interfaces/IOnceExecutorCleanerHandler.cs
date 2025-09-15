using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor.AspNetScheduler.Interfaces;

internal interface IOnceExecutorCleanerHandler
{
    /// <exception cref="OperationCanceledException"/>
    Task Execute(TimeSpan olderThan, CancellationToken cancellationToken = default);
}