namespace Dex.Lock.Async
{
    public interface IAsyncLockProviderFactory
    {
        IAsyncLockProvider<T> Create<T>(string instanceId = null);
    }
}