﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class ExecuteTransactionOutboxTests : BaseTest
    {
        private static readonly AsyncLocal<string> _asyncLocal = new();

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
                    await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);
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
                    var entity = new User
                    {
                        Name = outboxContext.State.Name,
                        Years = outboxContext.State.Age
                    };

                    await outboxContext.DbContext.Users.AddAsync(entity, token);

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
                    outboxContext.State.Logger.LogDebug("DEBUG...");

                    await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);
                    await outboxContext.EnqueueMessageAsync(new TestOutboxCommand2 { Args = "Command2" }, token);

                    return new TestOutboxCommand { Args = "hello world" };
                },
                (_, command) => Task.FromResult(command),
                CancellationToken.None);

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
                    await outboxContext.DbContext.Users.AddAsync(new User { Name = name }, token);

                    if (failureCount-- > 0)
                    {
                        TestContext.WriteLine("throw failure...");
                        throw new TimeoutException();
                    }

                    return new TestOutboxCommand { Args = "hello world" };
                },
                (_, command) => Task.FromResult(command),
                CancellationToken.None);

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await testDbContext.Users.AnyAsync(x => x.Name == name));
        }

        [Test]
        public async Task SimpleRunTestMultiThreaded()
        {
            var services = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();

            var threads = new HashSet<string>();
            using var ce = new CountdownEvent(3);

            TestDelayCommandHandler.OnProcess += (_, _) =>
            {
                lock (threads)
                {
                    threads.Add(_asyncLocal.Value);
                }

                ce.Signal();
            };

            const string arg1 = "hello world1";
            const string arg2 = "hello world2";
            const string arg3 = "hello world3";

            var outboxService = services.GetRequiredService<IOutboxService<TestDbContext>>();
            await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = arg1, DelayMsec = 5_000 });
            await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = arg2, DelayMsec = 5_000 });
            await outboxService.EnqueueAsync(new TestDelayOutboxCommand { Args = arg3, DelayMsec = 5_000 });
            await SaveChanges(services);

            async void ThreadEntry(string arg)
            {
                _asyncLocal.Value = arg;
                using var scope = services.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
                await handler.ProcessAsync();
            }

            var sw = Stopwatch.StartNew();
            ThreadPool.QueueUserWorkItem(_ => ThreadEntry(arg1));
            ThreadPool.QueueUserWorkItem(_ => ThreadEntry(arg2));
            ThreadPool.QueueUserWorkItem(_ => ThreadEntry(arg3));
            ce.Wait();
            sw.Stop();

            Assert.AreEqual(5, (int)sw.Elapsed.TotalSeconds);
            Assert.AreEqual(3, threads.Count);
        }
    }
}