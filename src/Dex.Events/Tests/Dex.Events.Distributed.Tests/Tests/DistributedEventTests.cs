﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
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
        [TestCase(true)]
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
                            busFactoryConfigurator
                                .SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(
                                    context, serviceName: "Test");
                        }
                        else
                        {
                            // alternate
                            context.RegisterReceiveEndpoint<OnUserAdded, TestOnUserAddedHandler>(busFactoryConfigurator,
                                createSeparateQueue: true);
                            context.RegisterReceiveEndpoint<OnUserAdded, TestOnUserAddedHandler2>(
                                busFactoryConfigurator, createSeparateQueue: true);
                        }
                    });
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnUserAddedHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandler2.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            await harness.StartAsync();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);

            await Task.Delay(100);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == user.Id));
            NUnit.Framework.Assert.That(count, Is.EqualTo(2 * 2));
            await harness.StopAsync();
        }

        [Test]
        public async Task RaiseDistributedEventOnlySendTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .RegisterDistributedEventRaiser()
                .AddMassTransitTestHarness()
                .BuildServiceProvider();

            var harness = serviceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = Guid.Empty }, CancellationToken.None);

            Assert.IsTrue(await harness.Published.Any<OnUserAdded>());
            Assert.IsTrue(harness.Published.Count() == 1);
            await harness.Stop();
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
                        x.UseMessageRetry(rc =>
                            rc.SetRetryPolicy(filter => filter.Interval(5, TimeSpan.FromMilliseconds(50)))));

                    c.UsingInMemory((context, configurator) =>
                        configurator
                            .SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler,
                                TestOnUserAddedHandlerRaiseException>(context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnUserAddedHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandlerRaiseException.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            await harness.StartAsync();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = user.Id }, CancellationToken.None);

            await Task.Delay(100);

            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == user.Id));
            NUnit.Framework.Assert.That(count, Is.EqualTo(1));
            await harness.StopAsync();
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
                        configurator
                            .SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(
                                context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestOnUserAddedHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandler2.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            await harness.StartAsync();

            var eventRaiser = serviceProvider.GetRequiredService<IDistributedEventRaiser<IBus>>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.Users.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await eventRaiser.RaiseAsync(new OnUserAdded { CustomerId = userId }, CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(2, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            await harness.StopAsync();
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
                        configurator
                            .SubscribeEventHandlers<OnUserAdded, TestOnUserAddedHandler, TestOnUserAddedHandler2>(
                                context));
                })
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandler2.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            await harness.StartAsync();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.ExecuteInTransactionScopeAsync(
                new { Entity = user, DbContext = dbContext, OutboxService = outboxService },
                async (state, token) =>
                {
                    var entity = state.Entity;
                    await state.DbContext.Users.AddAsync(entity, token);

                    await state.OutboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                        cancellationToken: token);
                    await state.OutboxService.EnqueueEventAsync(new OnUserAdded { CustomerId = entity.Id },
                        cancellationToken: token);
                    await state.DbContext.SaveChangesAsync(token);
                }, (_, _) => Task.FromResult(false));

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(3, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            await harness.StopAsync();
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
            TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOnUserAddedHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var harness = serviceProvider.GetRequiredService<IBusControl>();
            await harness.StartAsync();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.ExecuteInTransactionScopeAsync(
                new { Entity = user, DbContext = dbContext, OutboxService = outboxService },
                async (state, token) =>
                {
                    var entity = state.Entity;
                    await state.DbContext.Users.AddAsync(entity, token);

                    await state.OutboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                        cancellationToken: token);
                    await state.OutboxService.EnqueueEventAsync(new OnUserAdded { CustomerId = entity.Id },
                        cancellationToken: token);
                    await state.DbContext.SaveChangesAsync(token);
                }, (_, _) => Task.FromResult(false));

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(2, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
            await harness.StopAsync();
        }

        [Test]
        public async Task RaiseDistributedEventOnOutboxContextTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IBus>>,
                    TestOutboxDistributedEventHandler<IBus>>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IExternalBus>>,
                    TestOutboxDistributedEventHandler<IExternalBus>>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOutboxDistributedEventHandler<IBus>.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOutboxDistributedEventHandler<IExternalBus>.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };
            await dbContext.ExecuteInTransactionScopeAsync(
                new { Entity = user, DbContext = dbContext, OutboxService = outboxService },
                async (state, token) =>
                {
                    var entity = state.Entity;
                    await state.DbContext.Users.AddAsync(entity, token);

                    await state.OutboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                        cancellationToken: token);
                    await state.OutboxService.EnqueueEventAsync(new OnUserAdded { CustomerId = entity.Id },
                        cancellationToken: token);
                    await state.OutboxService.EnqueueEventAsync<IExternalBus>(
                        new OnUserAdded { CustomerId = entity.Id },
                        cancellationToken: token);
                    await state.DbContext.SaveChangesAsync(token);
                }, (_, _) => Task.FromResult(false));

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(3, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
        }

        [Test]
        public async Task RaiseDistributedEventOnOutboxServiceTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IBus>>,
                    TestOutboxDistributedEventHandler<IBus>>()
                .AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<IExternalBus>>,
                    TestOutboxDistributedEventHandler<IExternalBus>>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOutboxDistributedEventHandler<IBus>.OnProcess += (_, _) => Interlocked.Increment(ref count);
            TestOutboxDistributedEventHandler<IExternalBus>.OnProcess += (_, _) => Interlocked.Increment(ref count);

            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "juk_" + userId, Years = 25 };

            dbContext.Set<User>().Add(user);
            await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" });
            await outboxService.EnqueueEventAsync(new OnUserAdded { CustomerId = user.Id });
            await outboxService.EnqueueEventAsync<IExternalBus>(new OnUserAdded { CustomerId = user.Id });
            await dbContext.SaveChangesAsync();

            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(3, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Id == userId));
        }
    }
}