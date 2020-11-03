using Dex.Lock.Async;
using Dex.Lock.Async.Impl;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Lock.IntegrationalTest
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

            Thread.Sleep(1000);

            var releaser2 = await task1;        

            Thread.Sleep(1000);

            Task.Delay(2_000).ContinueWith(_ => releaser2.Dispose());

            var releaser3 = await task2;

            releaser3.Dispose();

            var locker4 = await locker.LockAsync();

            locker4.Dispose();

            //Task.Delay(3_000).ContinueWith(_ => releaser1.Dispose());

            //var pendingTask1 = locker.LockAsync();
            //var pendingTask2 = locker.LockAsync();

            //var releaser1 = await pendingTask1;

            //Debugger.Break();

            //releaser1.Dispose();

            //var releaser2 = await pendingTask2;

            //Debugger.Break();

            //releaser2.Dispose();
        }
    }
}
