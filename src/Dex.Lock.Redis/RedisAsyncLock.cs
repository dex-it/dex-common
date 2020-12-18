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
            var expired = DateTime.Now + Timeout;
            bool lockTacked;
            do
            {
                lockTacked = await _database
                    .LockTakeAsync(_key, string.Empty, Timeout, CommandFlags.DemandMaster)
                    .ConfigureAwait(false);

                if (lockTacked) break; // lock tacked

                await Task.Delay(10).ConfigureAwait(false);
            } while (expired > DateTime.Now);

            if (!lockTacked)
                throw new TimeoutException($"Lock can't tacked until timeout: {Timeout}");

            return new RedisLockReleaser(this);
        }

        internal Task<bool> RemoveLockObjectAsync()
        {
            return _database.LockReleaseAsync(_key, string.Empty, CommandFlags.DemandMaster);
        }

        internal bool RemoveLockObject()
        {
            return _database.LockRelease(_key, string.Empty, CommandFlags.DemandMaster);
        }
    }
}