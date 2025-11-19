using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces;

internal interface IOutboxCleanupDataProvider
{
    /// <returns>Число удалённых записей.</returns>
    /// <exception cref="OperationCanceledException"/>
    Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken);
}