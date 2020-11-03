using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Lock.Async.Impl;
using NUnit.Framework;

namespace Dex.Lock.TestProject
{
    public class AsyncLockTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task LockAsyncFuncTest1()
        {
            var list = new List<string>();

            var aLock = new AsyncLock();

            static Task AppendToListTask(ICollection<string> list)
            {
                return Task.Factory.StartNew(() => list.Add("Thread" + Thread.CurrentThread.ManagedThreadId));
            }

            var tasks = Enumerable.Range(1, 25)
                .Select(i => aLock.LockAsync(() => AppendToListTask(list)))
                .ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);


            foreach (var x in list)
            {
                TestContext.WriteLine(x);
            }
        }
    }
}