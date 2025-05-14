using Dex.Cap.OnceExecutor.Memory.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.Memory.Test
{
    public class OnceExecutorTest
    {
        [Test]
        public void ExecuteAsync_MultipleParallelActionsCalled_ModificatorCalledOnce()
        {
            var sp = AddOnceExecutor(5000);

            var executor = sp.GetRequiredService<IOnceExecutor<IOnceExecutorMemoryTransactionOptions, IDistributedCache>>();

            var idempotentKey = Guid.NewGuid().ToString("N");
            var toIncrement = 0;

            async void Body(int _)
            {
                await executor.ExecuteAsync(idempotentKey, (_, _) =>
                {
                    toIncrement++;
                    return Task.CompletedTask;
                });
            }

            Parallel.For(0, 1000, body: Body);
            Assert.That(toIncrement, Is.EqualTo(1));
        }

        [Test]
        public async Task CacheRemoves_AfterBeingExpired()
        {
            const int delay = 500;
            var sp = AddOnceExecutor(delay);

            var executor = sp.GetRequiredService<IOnceExecutor<IOnceExecutorMemoryTransactionOptions, IDistributedCache>>();
            var cache = sp.GetRequiredService<MemoryDistributedCache>();
            var idempotentKey = Guid.NewGuid().ToString("N");

            await executor.ExecuteAsync(idempotentKey, (_, _) => Task.CompletedTask);
            Assert.That(await cache.GetAsync($"lt-{idempotentKey}"), Is.Not.Null);

            await Task.Delay(delay);
            Assert.That(await cache.GetAsync($"lt-{idempotentKey}"), Is.Null);
        }

        private static ServiceProvider AddOnceExecutor(int timeout)
        {
            return new ServiceCollection()
                .AddOnceExecutor<MemoryDistributedCache>(
                    x => { x.SizeLimit = 100; },
                    y => { y.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(timeout); })
                .BuildServiceProvider();
        }
    }
}