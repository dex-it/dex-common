using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Events.Distributed.Extensions;
using Dex.Events.Distributed.OutboxExtensions.Extensions;
using Dex.Events.Distributed.OutboxExtensions;
using Dex.Events.Distributed.Tests.Events;
using Dex.Events.Distributed.Tests.Handlers;
using Dex.Events.Distributed.Tests.Models;
using Dex.Events.Distributed.Tests.Services;
using Dex.MassTransit.Rabbit;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Events.Distributed.Tests.Tests
{
    public class DistributedEventTests : BaseTest
    {
        [Test]
        public async Task RaiseDistributedEventMultipleRegistrationsTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransitInMemoryTestHarness(c =>
                {
                    c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler>();
                    c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler2>();
                    c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler, TestOnCardAddedHandler2>();
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnCardAddedHandler.OnProcessed += (_, _) => count++;
            TestOnCardAddedHandler2.OnProcessed += (_, _) => count++;

            using var harness = serviceProvider.GetRequiredService<InMemoryTestHarness>();
            await harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var name = "juk_" + Guid.NewGuid();
            var entity = new User { Name = name, Years = 25 };
            await dbContext.Users.AddAsync(entity, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnCardAdded { CardId = Guid.NewGuid() }, CancellationToken.None);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            Assert.That(harness.Consumed.Count(), Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(4));
            await harness.Stop();
        }

        [Test]
        public async Task RaiseDistributedEventAndPublishInMemoryTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransitInMemoryTestHarness(c => { c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler, TestOnCardAddedHandler2>(); })
                .BuildServiceProvider();

            using var harness = serviceProvider.GetRequiredService<InMemoryTestHarness>();
            await harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var name = "juk_" + Guid.NewGuid();
            var entity = new User { Name = name, Years = 25 };
            await dbContext.Users.AddAsync(entity, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnCardAdded { CardId = Guid.NewGuid() }, CancellationToken.None);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            Assert.That(await harness.Consumed.Any<OnCardAdded>());
            await harness.Stop();
        }

        [Test]
        public async Task RaiseOutboxDistributedEventTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IBus>>, TestOutboxDistributedEventHandler<IBus>>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IExternalBus>>, TestOutboxDistributedEventHandler<IExternalBus>>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestOutboxDistributedEventHandler<IBus>.OnProcess += (_, _) => { count++; };
            TestOutboxDistributedEventHandler<IExternalBus>.OnProcess += (_, _) => { count++; };

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var name = "juk_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Name = name, Age = 25 },
                async (token, outboxContext) =>
                {
                    var entity = new User { Name = outboxContext.State.Name, Years = outboxContext.State.Age };
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnCardAdded { CardId = Guid.NewGuid() }, token);
                    await outboxContext.RaiseDistributedEventAsync<IExternalBus>(new OnCardAdded { CardId = Guid.NewGuid() }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(3, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task RaiseOutboxDistributedEventAndPublishWithRegisterBusTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .RegisterOutboxDistributedEventHandler()
                .Configure<RabbitMqOptions>(_ => { })
                .AddMassTransitTestHarness(c =>
                {
                    c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler>();
                    c.RegisterBus((context, configurator) =>
                    {
                        context.RegisterDistributedEventSendEndPoint<OnCardAdded>();
                        context.RegisterDistributedEventReceiveEndpoint<OnCardAdded>(configurator);
                    });
                })
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var harness = serviceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var name = "juk_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Name = name, Age = 25 },
                async (token, outboxContext) =>
                {
                    var entity = new User { Name = outboxContext.State.Name, Years = outboxContext.State.Age };
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnCardAdded { CardId = Guid.NewGuid() }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            Assert.That(await harness.Consumed.Any<OnCardAdded>());
            await harness.Stop();
        }

        [Test]
        public async Task RaiseOutboxDistributedEventAndPublishInMemoryTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .RegisterOutboxDistributedEventHandler()
                .AddMassTransitInMemoryTestHarness(c => { c.RegisterDistributedEventHandlers<OnCardAdded, TestOnCardAddedHandler>(); })
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var harness = serviceProvider.GetRequiredService<InMemoryTestHarness>();
            await harness.Start();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var name = "juk_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Name = name, Age = 25 },
                async (token, outboxContext) =>
                {
                    var entity = new User { Name = outboxContext.State.Name, Years = outboxContext.State.Age };
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnCardAdded { CardId = Guid.NewGuid() }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            Assert.That(await harness.Consumed.Any<OnCardAdded>());
            await harness.Stop();
        }
    }
}