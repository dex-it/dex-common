using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    public sealed class RedisLockReleaser : IDisposable, IAsyncDisposable
    {
        private readonly RedisAsyncLock _redisAsyncLock;

        internal RedisLockReleaser(RedisAsyncLock redisAsyncLock)
        {
            _redisAsyncLock = redisAsyncLock;
        }

        public void Dispose()
        {
            _redisAsyncLock.RemoveLockObject();
        }

        public async ValueTask DisposeAsync()
        {
            await _redisAsyncLock.RemoveLockObjectAsync().ConfigureAwait(false);
        }
    }
}