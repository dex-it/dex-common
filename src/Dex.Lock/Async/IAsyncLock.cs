using System;
using System.Threading.Tasks;

namespace Dex.Lock.Async
{
    public interface IAsyncLock<T> where T : IDisposable
    {
        /// <summary>
        /// Take async lock, return after lock is taken
        /// </summary>
        /// <returns></returns>
        ValueTask<T> LockAsync();
    }
}