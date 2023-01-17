using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.OnceExecutor;

public abstract class SimpleOnceExecutor<TDbContext> : BaseOnceExecutor<Guid, TDbContext>
{
    protected sealed override async Task BeforeModification(Guid idempotentKey, CancellationToken cancellationToken)
    {
        await SaveIdempotentKey(idempotentKey, cancellationToken);
    }
    
    protected abstract Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken);
}