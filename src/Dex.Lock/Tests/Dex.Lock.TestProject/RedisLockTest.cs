using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Lock.Redis;
using NUnit.Framework;
using StackExchange.Redis;

namespace Dex.Lock.TestProject
{
    public class RedisLockTest
    {
        [Test]
        public async Task LockAsyncFuncTest1()
        {
            const int concurrent = 10;
            const int iterations = 10;

            var targetList = new List<string>();

            async Task AddToListSample()
            {
                var r = new Random((int) DateTime.UtcNow.Ticks);
                var lockerProvider = new RedisAsyncLockProvider(CreateDatabase(), "6D6AC302-23");
                var locker = lockerProvider.GetLocker("AddToListSample");

                await using (await locker.LockAsync().ConfigureAwait(false))
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        targetList.Add(Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture));
                        await Task.Delay(r.Next(5, 10)).ConfigureAwait(false);
                    }
                }
            }

            // act
            var tasks = Enumerable.Range(0, concurrent).Select(_ => AddToListSample()).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // check
            Assert.AreEqual(concurrent * iterations, targetList.Count);
        }

        private IDatabase CreateDatabase()
        {
            return ConnectionMultiplexer.Connect("localhost").GetDatabase();
        }
    }
}