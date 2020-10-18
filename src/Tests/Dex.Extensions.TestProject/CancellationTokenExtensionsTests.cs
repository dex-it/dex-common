using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Dex.Extensions.TestProject
{
    public class CancellationTokenExtensionsTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task CreateLinkedSourceWithTimeoutByTokenSourceTest1()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var cts1 = new CancellationTokenSource(200.MilliSeconds()))
                using (var cts2 = cts1.CreateLinkedSourceWithTimeout(100.MilliSeconds()))
                {
                    await Task.Delay(1.Seconds(), cts2.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                Assert.Less(sw.ElapsedMilliseconds, 200.MilliSeconds().Milliseconds);
                return;
            }
            
            Assert.Fail();
        }   
        
        [Test]
        public async Task CreateLinkedSourceWithTimeoutByTokenTest2()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var cts1 = new CancellationTokenSource(200.MilliSeconds()))
                using (var cts2 = cts1.Token.CreateLinkedSourceWithTimeout(100.MilliSeconds()))
                {
                    await Task.Delay(1.Seconds(), cts2.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                Assert.Less(sw.ElapsedMilliseconds, 200.MilliSeconds().Milliseconds);
                return;
            }
            
            Assert.Fail();
        }
    }
}