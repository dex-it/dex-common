namespace Dex.Lock.Async.Impl
{
    public class AsyncLockProviderFactory : IAsyncLockProviderFactory
    {
        public IAsyncLockProvider<T> Create<T>(string instanceId = null)
        {
            return new AsyncLockProvider<T>();
        }
    }
}