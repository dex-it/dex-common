﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class ExecuteTransactionOutboxTests : BaseTest
    {
        [Test]
        public async Task IdempotentRetryExecutionContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, IdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlationId,
                async (token, outboxContext) =>
                {
                    await outboxContext.EnqueueAsync(new TestUserCreatorCommand { UserName = name }, cancellationToken: token);
                },
                CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // repeat
            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            envelope.Status = OutboxMessageStatus.New;
            envelope.ScheduledStartIndexing = envelope.StartAtUtc;
            await SaveChanges(sp);
            await handler.ProcessAsync(CancellationToken.None);

            // check
            using var scope = sp.CreateScope();
            envelope = await GetDb(scope.ServiceProvider).Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(OutboxMessageStatus.Succeeded, envelope.Status);
        }

        [Test]
        public async Task NonIdempotentRetryExecutionContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, NonIdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlationId,
                async (token, outboxContext) =>
                {
                    await outboxContext.EnqueueAsync(new TestUserCreatorCommand { UserName = name }, cancellationToken: token);
                },
                CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // repeat
            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            envelope.Status = OutboxMessageStatus.New;
            envelope.ScheduledStartIndexing = envelope.StartAtUtc;
            await SaveChanges(sp);
            await handler.ProcessAsync(CancellationToken.None);

            // check
            using var scope = sp.CreateScope();
            envelope = await GetDb(scope.ServiceProvider).Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(OutboxMessageStatus.Failed, envelope.Status);
        }

        [Test]
        public async Task SimpleRunExecuteInTransactionWithoutContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlation = Guid.NewGuid();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlation,
                async (token, outboxContext) =>
                {
                    await outboxContext.DbContext.Users.AddAsync(new TestUser { Name = name }, token);
                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task SimpleRunExecuteInTransactionWithEmptyOutBoxMessageTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlationId,
                async (token, outboxContext) => { await outboxContext.DbContext.Users.AddAsync(new TestUser { Name = name }, token); }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.IsTrue(await outboxService.IsOperationExistsAsync(correlationId, CancellationToken.None));
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task SimpleRunExecuteInTransactionWithContextTest2()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlation = Guid.NewGuid();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlation, new { Name = name, Age = 25 },
                async (token, outboxContext) =>
                {
                    var entity = new TestUser
                    {
                        Name = outboxContext.State.Name,
                        Years = outboxContext.State.Age
                    };

                    await outboxContext.DbContext.Users.AddAsync(entity, token);
                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
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

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlation = Guid.NewGuid();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            var logger = sp.GetService<ILogger<EnqueueOutboxTests>>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlation, new { Logger = logger },
                async (token, outboxContext) =>
                {
                    outboxContext.State.Logger?.LogDebug("DEBUG...");

                    await outboxContext.DbContext.Users.AddAsync(new TestUser { Name = name }, token);
                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                    await outboxContext.EnqueueAsync(new TestOutboxCommand2 { Args = "Command2" }, cancellationToken: token);
                }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(2, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task RetryExecuteInTransactionTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlation = Guid.NewGuid();
            var testDbContext = sp.GetRequiredService<TestDbContext>();
            var failureCount = 2;

            // act
            var name = "mmx_" + Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlation,
                async (token, outboxContext) =>
                {
                    await outboxContext.DbContext.Users.AddAsync(new TestUser { Name = name }, token);

                    if (failureCount-- > 0)
                    {
                        TestContext.WriteLine("throw failure...");
                        throw new TimeoutException();
                    }

                    await outboxContext.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                }, CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await testDbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task ParallelProcessingMessagesTest()
        {
            // set process limit to 1
            var services = InitServiceCollection(1)
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();

            var threads = new ConcurrentBag<string>();
            using var ce = new CountdownEvent(3);

            TestDelayCommandHandler.OnProcess += (_, c) =>
            {
                threads.Add(c.Args);
                ce.Signal();
            };

            var num = 0;
            var correlationId = Guid.NewGuid();

            var outboxService = services.GetRequiredService<IOutboxService<TestDbContext>>();
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = GetId(), DelayMsec = 800 });
            await SaveChanges(services);

            var sw = Stopwatch.StartNew();
            ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());
            ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());
            ThreadPool.QueueUserWorkItem(_ => RunOutboxHandler());

            // wait for countdown
            ce.Wait();
            sw.Stop();

            Assert.AreEqual(num, threads.Distinct().Count());
            Assert.LessOrEqual((int)sw.Elapsed.TotalSeconds, 1);

            async void RunOutboxHandler()
            {
                using var scope = services.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                await handler.ProcessAsync();
            }

            string GetId() => (num++).ToString("D");
        }
    }
}