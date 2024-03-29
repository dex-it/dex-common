﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class EnqueueOutboxTests : BaseTest
    {
        [Test]
        public async Task SimpleRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var logger = sp.GetRequiredService<ILogger<EnqueueOutboxTests>>();
            var outboxService = sp.GetRequiredService<IOutboxService>();

            var correlationId = Guid.NewGuid();
            var messageIds = new List<Guid>();

            var command = new TestOutboxCommand { Args = "hello world" };
            messageIds.Add(command.MessageId);
            logger.LogInformation("Command1 {MessageId}", ((IOutboxMessage)command).MessageId);
            await outboxService.EnqueueAsync(correlationId, command);

            var command2 = new TestOutboxCommand { Args = "hello world2" };
            messageIds.Add(command2.MessageId);
            logger.LogInformation("Command2 {MessageId}", ((IOutboxMessage)command2).MessageId);

            await outboxService.EnqueueAsync(correlationId, command2);
            await SaveChanges(sp);

            var count = 0;

            TestCommandHandler.OnProcess += OnTestCommandHandlerOnOnProcess!;

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            TestCommandHandler.OnProcess -= OnTestCommandHandlerOnOnProcess!;
            Assert.AreEqual(2, count);

            void OnTestCommandHandlerOnOnProcess(object _, TestOutboxCommand m)
            {
                if (!messageIds.Contains(m.MessageId))
                {
                    throw new InvalidOperationException("MessageId not equals");
                }

                Interlocked.Increment(ref count);
                TestContext.WriteLine(Activity.Current?.Id);
            }
        }

        [Test]
        public async Task EmptyRunTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty);
            await outboxService.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty);
            await outboxService.EnqueueAsync(correlationId, EmptyOutboxMessage.Empty);
            await SaveChanges(sp);

            // act
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.IsTrue(db.Set<OutboxEnvelope>().Where(x => x.CorrelationId == correlationId).All(x => x.Status == OutboxMessageStatus.Succeeded));
        }

        [Test]
        public void FailedEmptyMessageIdRunTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            var correlationId = Guid.NewGuid();

            Assert.CatchAsync<InvalidOperationException>(async () => { await outboxService.EnqueueAsync(correlationId, new TestEmptyMessageId()); });
        }

        [Test]
        public async Task ErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();
            TestErrorCommandHandler.Reset();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = 2 });
            await SaveChanges(sp);

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(100);
            }

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(2, envelope.Retries);
            Assert.AreEqual(OutboxMessageStatus.Succeeded, envelope.Status);

            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task DatabaseErrorCommandFromHandlerRunTest()
        {
            // ошибка в БД в обработчике, по идее должна быть компенсация, но мы пока ничего не делаем

            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, NonIdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var id = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestUserCreatorCommand { Id = id });
            await outboxService.EnqueueAsync(correlationId, new TestUserCreatorCommand { Id = id });
            await SaveChanges(sp);

            // act
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var db = sp.GetRequiredService<TestDbContext>();
            var envelopes = await db.Set<OutboxEnvelope>().Where(x => x.CorrelationId == correlationId).ToArrayAsync();

            var failed = envelopes.Single(x => x.Status == OutboxMessageStatus.Failed);
            Assert.NotNull(failed);

            var success = envelopes.Single(x => x.Status == OutboxMessageStatus.Succeeded);
            Assert.NotNull(success);
        }

        [Test]
        public async Task DbContextIsolationTest()
        {
            // мусорим в дб контексте и падаем, следующий обработчик не должен вставить мусор из дб контекста

            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, NonIdempotentCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();

            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestUserCreatorCommand { Id = Guid.NewGuid() });
            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestUserCreatorCommand { Id = Guid.NewGuid() });
            await SaveChanges(sp);

            // act
            NonIdempotentCreateUserCommandHandler.CountDown = 1; // первая обработка упадет
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var db = sp.GetRequiredService<TestDbContext>();
            Assert.AreEqual(1, db.Set<TestUser>().Count());
        }

        [Test]
        public async Task NormalAndErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();
            TestErrorCommandHandler.Reset();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "hello" });
            await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = 1 });
            await SaveChanges(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(100);
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        [TestCase(1_000, 1)]
        [TestCase(60_000, 0)] // Может быть рассинхронизация времени с БД.
        public async Task CleanupSuccess(int olderThanMilliseconds, int expectedDeletedMessages)
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<TestDbContext>>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestOutboxCommand { Args = "hello world" });
            await SaveChanges(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            Assert.AreEqual(1, count);

            await Task.Delay(2000);

            var cleaner = sp.GetRequiredService<IOutboxCleanupDataProvider>();
            var deletedMessages = await cleaner.Cleanup(TimeSpan.FromMilliseconds(olderThanMilliseconds), CancellationToken.None);

            Assert.AreEqual(expectedDeletedMessages, deletedMessages);
        }

        [Test]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded)]
        [TestCase(1000, 0, OutboxMessageStatus.New)]
        public async Task EnqueueWithDelayedStart_ProcessMessage(int intervalMilliseconds, int expectedCount, OutboxMessageStatus expectedStatus)
        {
            var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();
            TestErrorCommandHandler.Reset();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "hello" }, DateTime.UtcNow.AddMilliseconds(intervalMilliseconds));
            await SaveChanges(serviceProvider);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = serviceProvider.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(100);
            }

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            var envelope = await dbContext.Set<OutboxEnvelope>().FirstAsync(x => x.CorrelationId == correlationId);
            Assert.AreEqual(expectedStatus, envelope.Status);

            Assert.AreEqual(expectedCount, count);
        }
    }
}