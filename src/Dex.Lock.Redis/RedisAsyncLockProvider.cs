using System;
using Dex.Lock.Async;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    public class RedisAsyncLockProvider : BaseLockProvider<RedisLockReleaser>
    {
        private readonly IDatabase _database;
        public override string InstanceKey { get; }

        public RedisAsyncLockProvider(IDatabase database, string instanceId)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            InstanceKey = CreateKey(instanceId);
        }

        public override IAsyncLock<RedisLockReleaser> GetLocker(string key)
        {
            return new RedisAsyncLock(_database, CreateKey(key));
        }
    }
}