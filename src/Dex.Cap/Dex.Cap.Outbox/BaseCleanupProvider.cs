using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Outbox;

public abstract class BaseCleanupProvider : IOutboxCleanupDataProvider
{
    public virtual Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}