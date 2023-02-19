using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class ConsumerTransactionScopeTests : BaseTest
    {
      
        [Test]
        public async Task OnceExecutorInMemoryTestHarnessTest1()
        {
            await using var provider = InitServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    // setup
                    cfg.AddConsumer<TestMessageConsumer>(configurator => configurator.ConcurrentMessageLimit = 20);
                })
                .BuildServiceProvider(true);

            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            var endpoint = await harness.GetConsumerEndpoint<TestMessageConsumer>();
            const int expected = 1000;
            var msgs = Enumerable.Range(1, expected).Select(x => new TestMessage { Id = Guid.NewGuid(), Name = "m" + x });
            await endpoint.SendBatch(msgs);

            await harness.InactivityTask;

            var db = provider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.AreEqual(expected, db.Users.LongCount());
        }

        [Test]
        public async Task OnceExecutorRabbitTest1()
        {
            const string queueName = "TestMessageConsumer";

            await using var provider = InitServiceCollection()
                .AddMassTransit(cfg =>
                {
                    const int concurrentMessageLimit = 25;
                    cfg.AddConsumer<TestMessageConsumer>(configurator => configurator.ConcurrentMessageLimit = concurrentMessageLimit);
                    cfg.UsingRabbitMq((context, configurator) =>
                    {
                        //
                        configurator.ReceiveEndpoint(queueName, cc =>
                        {
                            //
                            cc.ConfigureConsumer(context, typeof(TestMessageConsumer));
                            cc.ConcurrentMessageLimit = concurrentMessageLimit;
                        });
                    });
                })
                .BuildServiceProvider(true);

            var busControl = provider.GetRequiredService<IBusControl>();
            var messageCounter = new MessageCounter();
            busControl.ConnectConsumeMessageObserver(messageCounter);
            busControl.Start();

            try
            {
                const int expected = 1000;
                var msgs = Enumerable.Range(1, expected).Select(x => new TestMessage { Id = Guid.NewGuid(), Name = "m" + x });

                var endpoint = await busControl.GetSendEndpoint(new Uri("queue:" + queueName));

                await Task.WhenAll(msgs.Select(x => endpoint.Send(x)));

                var sw = Stopwatch.StartNew();
                while (messageCounter.Count < expected && sw.ElapsedMilliseconds < 5000)
                {
                    await Task.Delay(20);
                }

                var db = provider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
                Assert.AreEqual(expected, db.Users.LongCount());
            }
            finally
            {
                busControl.Stop();
            }
        }

        #region Consumer data

        public interface ITestMessage : IConsumer
        {
            Guid Id { get; }
            string Name { get; }
        }

        private class TestMessage : ITestMessage
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class TestMessageConsumer : IConsumer<ITestMessage>
        {
            private readonly IOnceExecutor<TestDbContext> _executor;

            public TestMessageConsumer(IOnceExecutor<TestDbContext> executor)
            {
                _executor = executor;
            }

            public async Task Consume(ConsumeContext<ITestMessage> context)
            {
                await _executor.ExecuteAsync(context.MessageId.ToString(), (dbContext, token) => CreateUser(context.Message, dbContext, token));
            }

            private static async Task CreateUser(ITestMessage m, TestDbContext dbContext, CancellationToken token)
            {
                dbContext.Users.Add(new TestUser { Id = m.Id, Name = m.Name });
                await dbContext.SaveChangesAsync(token);
            }
        }

        private class MessageCounter : IConsumeMessageObserver<ITestMessage>
        {
            private long _count;
            public long Count => _count;

            public Task PreConsume(ConsumeContext<ITestMessage> context)
            {
                return Task.CompletedTask;
            }

            public Task PostConsume(ConsumeContext<ITestMessage> context)
            {
                Interlocked.Increment(ref _count);
                return Task.CompletedTask;
            }

            public Task ConsumeFault(ConsumeContext<ITestMessage> context, Exception exception)
            {
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}