﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class EnqueueOutboxTests : BaseTest
    {
        private static readonly AsyncLocal<string> _asyncLocal = new();

        [Test]
        public async Task SimpleRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService>();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "hello world" }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "hello world2" }, CancellationToken.None);

            await SaveChanges(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) =>
            {
                count++;
                TestContext.WriteLine(Activity.Current?.Id);
            };

            var handler = sp.GetRequiredService<IOutboxHandler>();
            for (int i = 0; i < 5; i++)
            {
                await handler.ProcessAsync(CancellationToken.None);
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task ErrorCommandRunTest()
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
                .BuildServiceProvider();
            TestErrorCommandHandler.Reset();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestErrorOutboxCommand { MaxCount = 3 }, CancellationToken.None);
            await SaveChanges(sp);

            var count = 0;
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 3;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(50);
            }

            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task DatabaseErrorCommandFromHandlerRunTest()
        {
            // ошибка в БД в обработчике, по идее должна быть компенсация, но мы пока ничего не делаем

            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, TestCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();
            var id = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            await outboxService.EnqueueAsync(correlationId, new TestUserCreatorCommand { Id = id }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestUserCreatorCommand { Id = id }, CancellationToken.None);
            await SaveChanges(sp);

            // act
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var db = sp.GetRequiredService<TestDbContext>();
            var envelopes = await db.Set<OutboxEnvelope>().Where(x => x.CorrelationId == correlationId).ToArrayAsync();

            var failed = envelopes.Single(x => x.Status == OutboxMessageStatus.Failed);
            Assert.NotNull(failed);

            var succ = envelopes.Single(x => x.Status == OutboxMessageStatus.Succeeded);
            Assert.NotNull(succ);
        }

        [Test]
        public async Task DbContextIsolationTest()
        {
            // мусорим в дб контексте и падаем, следующий обработчик не должен вставить мусор из дб контекста

            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestUserCreatorCommand>, TestCreateUserCommandHandler>()
                .BuildServiceProvider();

            var outboxService = sp.GetRequiredService<IOutboxService<TestDbContext>>();

            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestUserCreatorCommand { Id = Guid.NewGuid() }, CancellationToken.None);
            await outboxService.EnqueueAsync(Guid.NewGuid(), new TestUserCreatorCommand { Id = Guid.NewGuid() }, CancellationToken.None);
            await SaveChanges(sp);

            // act
            TestCreateUserCommandHandler.CountDown = 1; // первая обработка упадет
            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync(CancellationToken.None);

            // assert
            var db = sp.GetRequiredService<TestDbContext>();
            Assert.AreEqual(1, db.Set<User>().Count());
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
            await outboxService.EnqueueAsync(correlationId, new TestOutboxCommand { Args = "hello" }, CancellationToken.None);
            await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = 1 }, CancellationToken.None);
            await SaveChanges(sp);

            var count = 0;
            TestCommandHandler.OnProcess += (_, _) => { count++; };
            TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
            var handler = sp.GetRequiredService<IOutboxHandler>();

            var repeat = 5;
            while (repeat-- > 0)
            {
                await handler.ProcessAsync(CancellationToken.None);
                await Task.Delay(20);
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
    }
}