using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor.Interfaces;

internal interface IOnceExecutorCleanupDataProvider
{
    /// <returns>Число удалённых записей.</returns>
    /// <exception cref="OperationCanceledException"/>
    Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken);
}