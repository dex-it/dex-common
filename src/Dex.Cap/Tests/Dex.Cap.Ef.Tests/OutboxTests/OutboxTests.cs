using System;
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
            var sp = new ServiceCollection()
                .AddScoped(_ => new TestDbContext(DbName))
                .AddOutbox<TestDbContext>()
                // handlers
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddLogging()
                .BuildServiceProvider();

            var client = sp.GetRequiredService<IOutboxService>();
            await client.Enqueue(new TestOutboxCommand() {Args = "hello world"}, Guid.NewGuid());
            await client.Enqueue(new TestOutboxCommand() {Args = "hello world2"}, Guid.NewGuid());

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.Process();
        }
    }
}