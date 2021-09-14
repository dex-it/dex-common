using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class OutboxTests : BaseTest
    {
        [Test]
        public async Task SimpleRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            await client.EnqueueAsync(new TestOutboxCommand {Args = "hello world"}, CancellationToken.None);
            await client.EnqueueAsync(new TestOutboxCommand {Args = "hello world2"}, CancellationToken.None);

            await Save(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task ErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            await client.EnqueueAsync(new TestErrorOutboxCommand {CountDown = 3}, CancellationToken.None);
            await Save(sp);

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(50);
            }

            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task NormalAndErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            await client.EnqueueAsync(new TestOutboxCommand {Args = "hello"}, CancellationToken.None);
            await client.EnqueueAsync(new TestErrorOutboxCommand {CountDown = 1}, CancellationToken.None);
            await Save(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(50);
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task SimpleRunExecuteInTransactionTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var client = sp.GetRequiredService<IOutboxService>();
            var correlation = Guid.NewGuid();
            var testDbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await client.ExecuteOperationAsync(correlation, async token =>
            {
                await testDbContext.Users.AddAsync(new User {Name = name}, token);
                return new TestOutboxCommand {Args = "hello world"};
            }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await testDbContext.Users.AnyAsync(x => x.Name == name));
        }
        
        [Test]
        public async Task SeveralOutboxMessagesInTransactionTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand2>, TestCommand2Handler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestCommand2Handler.OnProcess += (_, _) => { count++; };

            var client = sp.GetRequiredService<IOutboxService>();
            var correlation = Guid.NewGuid();
            var testDbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await client.ExecuteOperationAsync(correlation, async token =>
            {
                await testDbContext.Users.AddAsync(new User {Name = name}, token);
                await client.EnqueueAsync(Guid.NewGuid(), new TestOutboxCommand2(){Args = "Command2"}, CancellationToken.None);
                return new TestOutboxCommand {Args = "hello world"};
            }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(2, count);
            Assert.IsTrue(await testDbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task RetryExecuteInTransactionTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var client = sp.GetRequiredService<IOutboxService>();
            var correlation = Guid.NewGuid();
            var testDbContext = sp.GetRequiredService<TestDbContext>();
            var failureCount = 2;

            // act
            var name = "mmx_" + Guid.NewGuid();
            await client.ExecuteOperationAsync(correlation, async token =>
            {
                await testDbContext.Users.AddAsync(new User {Name = name}, token);

                if (failureCount-- > 0)
                {
                    TestContext.WriteLine("throw failure...");
                    throw new TimeoutException();
                }

                return new TestOutboxCommand {Args = "hello world"};
            }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await testDbContext.Users.AnyAsync(x => x.Name == name));
        }


        private IServiceCollection InitServiceCollection()
        {
            return new ServiceCollection()
                .AddLogging()
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext>();
        }

        private static async Task Save(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<TestDbContext>();
            await db.SaveChangesAsync();
        }
    }
}