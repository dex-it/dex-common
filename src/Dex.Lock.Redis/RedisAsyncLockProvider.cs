using System;
using System.Threading.Tasks;
using Dex.Lock.Async;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    public class RedisAsyncLockProvider<T> : IAsyncLockProvider<T>
    {
        private readonly IDatabase _database;
        private readonly string _instanceId;

        public RedisAsyncLockProvider(IDatabase database, string? instanceId = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _instanceId = instanceId ?? Guid.NewGuid().ToString("N");
        }

        public IAsyncLock GetLock(T key)
        {
            return new RedisAsyncLock(_database, CreateKey(key));
        }

        [Obsolete("Проверить снимается ли блокировка если обрыв соединения")]
        public Task<bool> RemoveLock(T key)
        {
            var asyncLock = new RedisAsyncLock(_database, CreateKey(key));
            return asyncLock.RemoveLockObject();
        }

        private string CreateKey(T key)
        {
            return _instanceId + key;
        }
    }
}