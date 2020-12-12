using System;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLock<T> where T : IDisposable
    {
        ValueTask<T> LockAsync();
    }
}