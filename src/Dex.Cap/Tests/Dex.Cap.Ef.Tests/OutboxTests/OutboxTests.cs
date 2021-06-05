﻿using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox;
using Dex.Cap.Outbox.Ef;
using Dex.Outbox.Command.Test;
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
            await client.Enqueue(new TestOutboxCommand() {Args = "hello world"}, CancellationToken.None);
            await client.Enqueue(new TestOutboxCommand() {Args = "hello world2"}, CancellationToken.None);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.Process(CancellationToken.None);

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task ErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            await client.Enqueue(new TestErrorOutboxCommand {CountDown = 3}, CancellationToken.None);

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.Process(CancellationToken.None);
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
            await client.Enqueue(new TestOutboxCommand() {Args = "hello"}, CancellationToken.None);
            await client.Enqueue(new TestErrorOutboxCommand {CountDown = 1}, CancellationToken.None);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.Process(CancellationToken.None);
                await Task.Delay(50);
            }

            Assert.AreEqual(2, count);
        }

        private IServiceCollection InitServiceCollection()
        {
            return new ServiceCollection()
                .AddLogging()
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext>();
        }
    }
}