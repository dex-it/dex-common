using System;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProviderFactory<TR> where TR : IDisposable
    {
        IAsyncLockProvider<T, TR> Create<T>(string? instanceId = null);
    }
}