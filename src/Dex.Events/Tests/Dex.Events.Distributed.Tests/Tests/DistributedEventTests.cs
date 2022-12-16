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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Events.Distributed.Tests.Tests
{
    public class DistributedEventTests : BaseTest
    {
        [TestCase(true)]
        // [TestCase(false)]
        public async Task RaiseDistributedEventMultipleRegistrationsTest(bool isMainApproach)
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransit(c =>
                {
                    c.RegisterAllEventHandlers();
                    c.UsingRabbitMq((context, busFactoryConfigurator) =>
                    {
                        // main approach
                        if (isMainApproach)
                        {
                            busFactoryConfigurator.SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(context,
                                serviceName: "Test");
                        }
                        else
                        {
                            // alternate
                            context.RegisterReceiveEndpoint<TestOnUserAddedHandler, OnUserAdded>(busFactoryConfigurator, createSeparateQueue: true);
                            context.RegisterReceiveEndpoint<TestOnUserAddedHandler2, OnUserAdded>(busFactoryConfigurator, createSeparateQueue: true);
                        }
                    });
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnUserAddedHandler.OnProcessed += (_, _) => count++;
            TestOnUserAddedHandler2.OnProcessed += (_, _) => count++;

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == user.Id));
            Assert.That(count, Is.EqualTo(2 * 2));
            harness.Stop();
        }

        [Test]
        public async Task RaiseDistributedEventWithHandlerRaiseExceptionTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransit(c =>
                {
                    c.RegisterEventHandler<TestOnUserAddedHandler>(_ => { });
                    c.RegisterEventHandler<TestOnUserAddedHandlerRaiseException>(x =>
                        x.UseRetry(rc => rc.SetRetryPolicy(filter => filter.Interval(5, TimeSpan.FromMilliseconds(50)))));
                    
                    c.UsingInMemory((context, configurator) =>
                        configurator.SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandlerRaiseException>(context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnUserAddedHandler.OnProcessed += (_, _) => count++;
            TestOnUserAddedHandlerRaiseException.OnProcessed += (_, _) => count++;

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == user.Id));
            Assert.That(count, Is.EqualTo(1));
            harness.Stop();
        }

        [Test]
        public async Task RaiseDistributedEventAndPublishInMemoryTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransit(c =>
                {
                    c.RegisterAllEventHandlers();
                    c.UsingInMemory((context, configurator) =>
                        configurator.SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(context));
                })
                .BuildServiceProvider();

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = userId }, CancellationToken.None);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            harness.Stop();
        }

        [Test]
        public async Task RaiseOutboxDistributedEventAndPublishWithRegisterBusTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .RegisterOutboxDistributedEventHandler()
                .Configure<RabbitMqOptions>(_ => { })
                .AddMassTransit(c =>
                {
                    c.RegisterAllEventHandlers();
                    c.UsingInMemory((context, configurator) =>
                        configurator.SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            harness.Start();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Entity = user },
                async (token, outboxContext) =>
                {
                    var entity = outboxContext.State.Entity;
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnUserAdded { CustomerId = entity.Id }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            harness.Stop();
        }

        [Test]
        public async Task RaiseOutboxDistributedEventAndPublishInMemoryTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .RegisterOutboxDistributedEventHandler()
                .AddMassTransit(c =>
                {
                    c.RegisterAllEventHandlers();
                    c.UsingInMemory((context, configurator) =>
                        configurator.SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler>(context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            harness.Start();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Entity = user },
                async (token, outboxContext) =>
                {
                    var entity = outboxContext.State.Entity;
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnUserAdded { CustomerId = entity.Id }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            harness.Stop();
        }

        [Test]
        public async Task RaiseDistributedEventOnOutboxContextTest()
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

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await outboxService.ExecuteOperationAsync(Guid.NewGuid(), new { Entity = user },
                async (token, outboxContext) =>
                {
                    var entity = outboxContext.State.Entity;
                    await outboxContext.DbContext.Users.AddAsync(entity, token);

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, token);
                    await outboxContext.RaiseDistributedEventAsync(new OnUserAdded { CustomerId = entity.Id }, token);
                    await outboxContext.RaiseDistributedEventAsync<IExternalBus>(new OnUserAdded { CustomerId = entity.Id }, token);
                }, CancellationToken.None);

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(3, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
        }

        [Test]
        public async Task RaiseDistributedEventOnOutboxServiceTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IBus>>, TestOutboxDistributedEventHandler<IBus>>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IExternalBus>>, TestOutboxDistributedEventHandler<IExternalBus>>()
                .BuildServiceProvider();

            var count = 0;
            TestOutboxDistributedEventHandler<IBus>.OnProcess += (_, _) => { count++; };
            TestOutboxDistributedEventHandler<IExternalBus>.OnProcess += (_, _) => { count++; };

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };

            dbContext.Set<User>().Add(user);
            var correlationId = Guid.NewGuid();
            await outboxService.RaiseDistributedEventAsync(correlationId, new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);
            await outboxService.RaiseDistributedEventAsync<IExternalBus>(correlationId, new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);
            await dbContext.SaveChangesAsync();

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            Assert.AreEqual(2, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
        }
    }
}