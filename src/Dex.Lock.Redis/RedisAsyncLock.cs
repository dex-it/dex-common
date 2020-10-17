using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dex.Lock.Async;
using StackExchange.Redis;

namespace Dex.Lock.Redis
{
    internal class RedisAsyncLock : IAsyncLock
    {
        private const int MaxTries = 50;
        private const int IterationDelay = 50;
        private readonly IDatabase _database;
        private readonly string _key;

        internal RedisAsyncLock([NotNull] IDatabase database, [NotNull] string key)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public async Task LockAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));
            var c = MaxTries;
            while (c-- > 0)
            {
                var success = await _database.LockTakeAsync(_key, string.Empty, TimeSpan.MaxValue);
                if (success)
                {
                    try
                    {
                        await asyncAction();
                    }
                    finally
                    {
                        await RemoveLockObject();
                    }

                    return;
                }

                await Task.Delay(IterationDelay);
            }

            throw new TimeoutException("[" + IterationDelay * MaxTries + "ms]");
        }

        internal Task<bool> RemoveLockObject()
        {
            return _database.LockReleaseAsync(_key, string.Empty);
        }

        public async Task LockAsync(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Task Act()
            {
                action();
                return Task.FromResult(true);
            }

            await LockAsync(Act);
        }
    }
}