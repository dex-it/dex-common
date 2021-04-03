using Dex.Lock.Async;
using Dex.Lock.Async.Impl;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Lock.DevelopmentTest
{
    class Program
    {
        static async Task Main()
        {
            var locker = new AsyncLock();

            LockReleaser releaser1 = await locker.LockAsync();

            var task1 = locker.LockAsync();
            var task2 = locker.LockAsync();

            releaser1.Dispose();
            releaser1.Dispose();

            Thread.Sleep(1000);

            var releaser2 = await task1;
            releaser1.Dispose();

            Thread.Sleep(1000);

#pragma warning disable 4014
            Task.Delay(2_000).ContinueWith(_ => releaser2.Dispose());
#pragma warning restore 4014

            var releaser3 = await task2;

            releaser3.Dispose();

            var locker4 = await locker.LockAsync();

            locker4.Dispose();
        }
    }
}