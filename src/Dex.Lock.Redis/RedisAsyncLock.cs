using System;
using System.Threading.Tasks;
using Dex.Lock.Async;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    internal class RedisAsyncLock : IAsyncLock<RedisLockReleaser>
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        private readonly IDatabase _database;
        private readonly string _key;

        // TODO check database for support, dirty connection close, Danilov
        internal RedisAsyncLock(IDatabase database, string key)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public async ValueTask<RedisLockReleaser> LockAsync()
        {
            await _database.LockTakeAsync(_key, string.Empty, Timeout).ConfigureAwait(false);
            return new RedisLockReleaser(this);
        }

        internal Task<bool> RemoveLockObjectAsync()
        {
            return _database.LockReleaseAsync(_key, string.Empty);
        }

        internal bool RemoveLockObject()
        {
            return _database.LockRelease(_key, string.Empty);
        }
    }
}