using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    internal abstract class BaseCleanupProvider : IOutboxCleanupDataProvider
    {
        public virtual Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
