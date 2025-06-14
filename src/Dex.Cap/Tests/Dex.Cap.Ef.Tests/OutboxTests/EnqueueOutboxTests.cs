﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.OutboxTests.Handlers;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Extensions;
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

            var messageIds = new List<Guid>();
            var command = new TestOutboxCommand { Args = "hello world" };
            messageIds.Add(command.TestId);
            logger.LogInformation("Command1 {MessageId}", command.TestId);
            await outboxService.EnqueueAsync(command);

            var command2 = new TestOutboxCommand { Args = "hello world2" };
            messageIds.Add(command2.TestId);
            logger.LogInformation("Command2 {MessageId}", command2.TestId);

            await outboxService.EnqueueAsync(command2);
            await SaveChanges(sp);

            var count = 0;

            TestCommandHandler.OnProcess += OnTestCommandHandlerOnOnProcess!;

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            TestCommandHandler.OnProcess -= OnTestCommandHandlerOnOnProcess!;
            Assert.AreEqual(2, count);

            void OnTestCommandHandlerOnOnProcess(object _, TestOutboxCommand m)
            {
                if (!messageIds.Contains(m.TestId))
                {
                    throw new InvalidOperationException("MessageId not equals");
                }

                Interlocked.Increment(ref count);
                TestContext.WriteLine(Activity.Current?.Id);
            }
        }

        [Test]
        public void FailedEnqueueMessageWithoutDiscriminator()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();

            NUnit.Framework.Assert.CatchAsync<DiscriminatorResolveException>(async () =>
            {
                await outboxService.EnqueueAsync(new TestEmptyMessageId());
            });
        }

        [Test]
        public async Task ErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            TestErrorCommandHandler.Reset();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            await outboxService.EnqueueAsync(new TestErrorOutboxCommand { MaxCount = 2 });
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

            var dbContext = sp.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var envelope = await dbContext.Set<OutboxEnvelope>()
                .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
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

            var outboxService = sp.GetRequiredService<IOutboxService>();
            var id = Guid.NewGuid();
            await outboxService.EnqueueAsync(new TestUserCreatorCommand { Id = id });
            await outboxService.EnqueueAsync(new TestUserCreatorCommand { Id = id });
            await SaveChanges(sp);

            // act
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var envelopes = await db.Set<OutboxEnvelope>()
                .Where(x => x.CorrelationId == outboxService.CorrelationId).ToArrayAsync();

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

            var outboxService = sp.GetRequiredService<IOutboxService>();

            await outboxService.EnqueueAsync(new TestUserCreatorCommand { Id = Guid.NewGuid() });
            await outboxService.EnqueueAsync(new TestUserCreatorCommand { Id = Guid.NewGuid() });
            await SaveChanges(sp);

            // act
            NonIdempotentCreateUserCommandHandler.CountDown = 1; // первая обработка упадет
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var db = sp.GetRequiredService<TestDbContext>();
            Assert.AreEqual(1, await db.Set<TestUser>().CountAsync());
        }

        [Test]
        public async Task NormalAndErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();

            TestErrorCommandHandler.Reset();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello" });
            await outboxService.EnqueueAsync(new TestErrorOutboxCommand { MaxCount = 1 });
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

            var outboxService = sp.GetRequiredService<IOutboxService>();
            await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" });
            await SaveChanges(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            Assert.AreEqual(1, count);

            await Task.Delay(2000);

            var cleaner = sp.GetRequiredService<IOutboxCleanupDataProvider>();
            var deletedMessages =
                await cleaner.Cleanup(TimeSpan.FromMilliseconds(olderThanMilliseconds), CancellationToken.None);

            Assert.AreEqual(expectedDeletedMessages, deletedMessages);
        }

        [Test]
        [TestCase(100, 1, OutboxMessageStatus.Succeeded)]
        [TestCase(1000, 0, OutboxMessageStatus.New)]
        public async Task EnqueueWithDelayedStart_ProcessMessage(int intervalMilliseconds, int expectedCount,
            OutboxMessageStatus expectedStatus)
        {
            var serviceProvider = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            TestErrorCommandHandler.Reset();

            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();
            await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello" },
                startAtUtc: DateTime.UtcNow.AddMilliseconds(intervalMilliseconds));
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

            var dbContext = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var envelope = await dbContext.Set<OutboxEnvelope>()
                .FirstAsync(x => x.CorrelationId == outboxService.CorrelationId);
            Assert.AreEqual(expectedStatus, envelope.Status);

            Assert.AreEqual(expectedCount, count);
        }

        [Test(Description = "Нельзя задать LockTimeout меньше 10сек")]
        public Task CantSetLockTimeoutLess10SecTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();

            var command = new TestDelayOutboxCommand { Args = "delay test", DelayMsec = 6_000 };

            NUnit.Framework.Assert.CatchAsync<ArgumentOutOfRangeException>(async () =>
            {
                // LockTimeout must be greater 10sec
                await outboxService.EnqueueAsync(command, lockTimeout: 9.Seconds());
            });

            return Task.CompletedTask;
        }

        [Test(Description =
            "Сообщение не может быть обработано, т.к. на него нужно больше времени чем предусматривает LockTimeout")]
        public async Task LargeProcessingTimeLockTimeoutExceededTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();

            var command = new TestDelayOutboxCommand { Args = "delay test", DelayMsec = 6_000 };
            // реальное время на выполнение будет 10-5=5сек, 5сек - отведено на завершение операции и нивелирование конкуренции
            var id = await outboxService.EnqueueAsync(command, lockTimeout: 10.Seconds());
            await SaveChanges(sp);

            // run
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            using var scope = sp.CreateScope();
            var e = await GetDb(scope.ServiceProvider).Set<OutboxEnvelope>().FindAsync(id);
            Assert.AreEqual(1, e?.Retries);
            Assert.AreEqual(OutboxMessageStatus.Failed, e?.Status);
        }

        [Test(Description = "Обработка нескольких сообщений общей продолжительностью больше чем LockTimeout, " +
                            "приведет к отмене обработки задач за пределами кванта времени")]
        public async Task LargeProcessingTimeSeveralMessagesTest()
        {
            var sp = InitServiceCollection(5)
                .AddScoped<IOutboxMessageHandler<TestDelayOutboxCommand>, TestDelayCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();

            var id1 = await outboxService.EnqueueAsync(
                new TestDelayOutboxCommand { Args = "delay test-1", DelayMsec = 2_000 }, lockTimeout: 10.Seconds());
            var id2 = await outboxService.EnqueueAsync(
                new TestDelayOutboxCommand { Args = "delay test-2", DelayMsec = 2_000 }, lockTimeout: 10.Seconds());
            var id3 = await outboxService.EnqueueAsync(
                new TestDelayOutboxCommand { Args = "delay test-3", DelayMsec = 5_000 }, lockTimeout: 10.Seconds());
            await SaveChanges(sp);

            // run
            var sw = Stopwatch.StartNew();
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // check
            using var scope = sp.CreateScope();
            var envelopes = await GetDb(scope.ServiceProvider).Set<OutboxEnvelope>().ToArrayAsync();

            Assert.AreEqual(OutboxMessageStatus.Succeeded, envelopes.First(x => x.Id == id1).Status);
            Assert.AreEqual(OutboxMessageStatus.Succeeded, envelopes.First(x => x.Id == id2).Status);
            Assert.AreEqual(OutboxMessageStatus.Failed, envelopes.First(x => x.Id == id3).Status);

            // проверяем что обработка закончилась сразу по прошествии LockTimeout для всех выбранных сообщений
            Assert.Less(sw.Elapsed, TimeSpan.FromSeconds(2 + 2 + 5));
        }
    }
}