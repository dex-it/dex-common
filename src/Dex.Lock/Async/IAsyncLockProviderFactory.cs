using System;

namespace Dex.Lock.Async
{
    public interface IAsyncLockProviderFactory<TR> where TR : IDisposable
    {
        /// <summary>
        /// Create IAsyncLockProvider with unique InstanceId, instanceId is a scope for set of locks
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        IAsyncLockProvider<T, TR> Create<T>(string? instanceId = null);
    }
}