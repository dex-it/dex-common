using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.DistributedEvents.Tests.Events;
using Dex.Events.DistributedEvents.Tests.Models;
using Dex.Events.OutboxDistributedEvents.Extensions;
using DistributedEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Events.DistributedEvents.Tests.Tests
{
    public class DistributedEventTests : BaseTest
    {
        [Test]
        public async Task SimpleRaiseDistributedEventTest()
        {
            var sp = InitServiceCollection()
                .RegisterOutboxDistributedEventHandler()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "juk_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Name = name, Age = 25 },
                async (token, outboxContext) =>
                {
                    var entity = new User { Name = outboxContext.State.Name, Years = outboxContext.State.Age };
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.RaiseDistributedEventAsync<TestDbContext, object, IBus, DistributedBaseEventParams>(
                        new OnCardAdded { CustomerId = Guid.NewGuid() }, cancellationToken: token);

                    return new TestOutboxCommand { Args = "hello world" };
                },
                (_, command) => Task.FromResult(command),
                CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }
    }
}