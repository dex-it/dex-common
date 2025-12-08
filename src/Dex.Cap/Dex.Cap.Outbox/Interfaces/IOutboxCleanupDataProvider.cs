using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxCleanupDataProvider
{
    /// <returns>Число удалённых записей.</returns>
    /// <exception cref="OperationCanceledException"/>
    Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken = default);
}