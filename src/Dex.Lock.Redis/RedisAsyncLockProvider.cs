using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dex.Lock.Async;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    public class RedisAsyncLockProvider<T> : IAsyncLockProvider<T>
    {
        private readonly IDatabase _database;
        private readonly string _instanceId;

        public RedisAsyncLockProvider([NotNull] IDatabase database, string instanceId = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _instanceId = instanceId ?? Guid.NewGuid().ToString("N");
        }

        public IAsyncLock Get(T key)
        {
            return new RedisAsyncLock(_database, CreateKey(key));
        }

        public Task<bool> Remove(T key)
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