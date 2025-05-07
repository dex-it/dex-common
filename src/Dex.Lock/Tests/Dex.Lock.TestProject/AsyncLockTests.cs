using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dex.Lock.Async.Impl;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
            var isError = false;

            Task AppendToListTask(ICollection<string> collection)
            {
                return Task.Run(() =>
                {
                    try
                    {
                        collection.Add("Thread" + Environment.CurrentManagedThreadId);
                    }
                    catch (Exception)
                    {
                        isError = true;
                    }
                });
            }

            const int expected = 256;
            var tasks = Enumerable.Range(1, expected)
                .Select(_ => aLock.LockAsync(() => AppendToListTask(list)))
                // .Select(_ => AppendToListTask(list))
                .ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            ClassicAssert.AreEqual(expected, list.Count);
            ClassicAssert.IsFalse(isError);
        }
    }
}