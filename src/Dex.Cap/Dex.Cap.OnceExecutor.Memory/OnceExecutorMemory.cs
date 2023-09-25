using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Dex.Cap.OnceExecutor.Memory;

public class OnceExecutorMemory<TDistributedCache> : BaseOnceExecutor<IMemoryOptions, TDistributedCache>
    where TDistributedCache : class, IDistributedCache
{
    private const string KeyPrefix = "lt";

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly IOptions<DistributedCacheEntryOptions> _defaultCacheEntryOptions;

    public OnceExecutorMemory(IOptions<DistributedCacheEntryOptions> defaultCacheEntryOptions,
        TDistributedCache context)
    {
        _defaultCacheEntryOptions = defaultCacheEntryOptions ??
                                    throw new ArgumentNullException(nameof(defaultCacheEntryOptions));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected override TDistributedCache Context { get; }

    protected override async Task<TResult?> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult?>> operation, Func<CancellationToken, Task<bool>> verifySucceeded,
        IMemoryOptions? options,
        CancellationToken cancellationToken) where TResult : default
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey,
        CancellationToken cancellationToken)
    {
        return await Context.GetAsync(GetKey(idempotentKey), cancellationToken).ConfigureAwait(false) != null;
    }

    protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
    {
        await Context.SetAsync(GetKey(idempotentKey), Array.Empty<byte>(),
            _defaultCacheEntryOptions.Value, cancellationToken).ConfigureAwait(false);
    }

    protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string GetKey(string idempotentKey)
    {
        return $"{KeyPrefix}-{idempotentKey}";
    }
}