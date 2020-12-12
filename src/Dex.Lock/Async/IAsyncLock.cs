using System;
using System.Threading.Tasks;
using Dex.Lock.Async.Impl;

namespace Dex.Lock.Async
{
    public interface IAsyncLock<T> where T : IDisposable
    {
        ValueTask<T> LockAsync();
    }
}