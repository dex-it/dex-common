using System;
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
        public async Task InMemoryTestHarnessTest1()
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
            const int expected = 100;
            var msgs = Enumerable.Range(1, expected).Select(x => new TestMessage { Id = Guid.NewGuid(), Name = "m" + x });
            await endpoint.SendBatch(msgs);

            await harness.InactivityTask;

            var db = provider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.AreEqual(expected, db.Users.LongCount());
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

        #endregion
    }
}