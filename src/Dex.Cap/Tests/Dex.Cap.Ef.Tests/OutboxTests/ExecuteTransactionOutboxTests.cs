using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox;
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
        private static readonly AsyncLocal<string> AsyncLocal = new();

        [Test]
        public async Task IdempotentRetryExecutionContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, IdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Cap.Ef.Tests.OutboxTests.Handlers.TestUserCreatorCommand, Dex.Cap.Ef.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            var msgId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlationId,
                async (token, outboxContext) =>
                {
                    await outboxContext.EnqueueAsync(new TestUserCreatorCommand { MessageId = msgId, UserName = name }, cancellationToken: token);
                },
                CancellationToken.None);

            // IdempotentCreateUserCommandHandler.CountDown = 2;

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // repeat
            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            envelope.Status = OutboxMessageStatus.New;
            envelope.ScheduledStartIndexing = envelope.StartAtUtc;
            await SaveChanges(sp);
            await handler.ProcessAsync(CancellationToken.None);

            // check
            envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(OutboxMessageStatus.Succeeded, envelope.Status);
        }

        [Test]
        public async Task NonIdempotentRetryExecutionContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, NonIdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Cap.Ef.Tests.OutboxTests.Handlers.TestUserCreatorCommand, Dex.Cap.Ef.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            var msgId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.ExecuteOperationAsync(correlationId,
                async (token, outboxContext) =>
                {
                    await outboxContext.EnqueueAsync(new TestUserCreatorCommand { MessageId = msgId, UserName = name }, cancellationToken: token);
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
            envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(OutboxMessageStatus.Failed, envelope.Status);
        }

        [Test]
        public async Task SimpleRunExecuteInTransactionWithoutContextTest1()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Outbox.Command.Test.TestOutboxCommand, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Cap.Outbox.Models.EmptyOutboxMessage, Dex.Cap.Outbox, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Outbox.Command.Test.TestOutboxCommand, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Outbox.Command.Test.TestOutboxCommand, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            d.Add("2", "Dex.Outbox.Command.Test.TestOutboxCommand2, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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
            
            var d = sp.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Outbox.Command.Test.TestOutboxCommand, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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
        public async Task SimpleRunTestMultiThreaded()
        {
            var services = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();
            
            var d = services.GetRequiredService<OutboxTypeDiscriminator<string>>();
            d.Add("1", "Dex.Outbox.Command.Test.TestDelayOutboxCommand, Dex.Outbox.Command.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

            var threads = new HashSet<string>();
            using var ce = new CountdownEvent(3);

            TestDelayCommandHandler.OnProcess += (_, _) =>
            {
                lock (threads)
                {
                    threads.Add(AsyncLocal.Value!);
                }

                // ReSharper disable once AccessToDisposedClosure
                ce.Signal();
            };

            const string arg1 = "hello world1";
            const string arg2 = "hello world2";
            const string arg3 = "hello world3";

            var outboxService = services.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = arg1, DelayMsec = 5_000 });
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = arg2, DelayMsec = 5_000 });
            await outboxService.EnqueueAsync(correlationId, new TestDelayOutboxCommand { Args = arg3, DelayMsec = 5_000 });
            await SaveChanges(services);

            async void ThreadEntry(string arg)
            {
                lock (threads)
                {
                    AsyncLocal.Value = arg;
                }

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