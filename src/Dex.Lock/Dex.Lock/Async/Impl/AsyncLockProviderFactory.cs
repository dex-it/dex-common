namespace Dex.Lock.Async.Impl
{
    public class AsyncLockProviderFactory : IAsyncLockProviderFactory<LockReleaser>
    {
        public IAsyncLockProvider<T, LockReleaser> Create<T>(string? instanceId) where T : notnull
        {
            return new AsyncLockProvider<T>();
        }
    }
}