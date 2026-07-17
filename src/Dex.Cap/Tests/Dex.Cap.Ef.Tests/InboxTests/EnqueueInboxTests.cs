using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

public class EnqueueInboxTests : BaseTest
{
    [Test]
    public async Task EnqueueAndProcess_MessageIsHandledOnceAndMarkedSucceeded()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var status = await inboxService.EnqueueAsync(
            new TestInboxCommand { Args = "hello world" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        Assert.AreEqual(InboxEnqueueStatus.Accepted, status);

        var processedCount = 0;
        TestInboxCommandHandler.OnProcess += OnProcess;
        try
        {
            var handler = sp.GetRequiredService<IInboxHandler>();
            var processed = await handler.ProcessAsync(CancellationToken.None);
            Assert.AreEqual(1, processed);
        }
        finally
        {
            TestInboxCommandHandler.OnProcess -= OnProcess;
        }

        Assert.AreEqual(1, processedCount);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status);
        Assert.AreEqual(0, envelope.Retries);
        // Завершённое сообщение выведено из выборки и из частичного индекса, а аренда отпущена.
        Assert.IsNull(envelope.ScheduledStartIndexing);
        Assert.IsNull(envelope.LockId);

        void OnProcess(object? _, TestInboxCommand __) => Interlocked.Increment(ref processedCount);
    }

    [Test]
    public async Task Enqueue_SameMessageIdAndConsumer_IsRejectedAsDuplicate()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var identity = new InboxMessageIdentity("message-1", "consumer-1");

        var first = await inboxService.EnqueueAsync(new TestInboxCommand { Args = "first" }, identity);
        var second = await inboxService.EnqueueAsync(new TestInboxCommand { Args = "redelivery" }, identity);

        Assert.AreEqual(InboxEnqueueStatus.Accepted, first);
        Assert.AreEqual(InboxEnqueueStatus.Duplicate, second);

        // Повтор не создаёт вторую строку: иначе бизнес-эффект применился бы дважды.
        var envelopes = await GetDb(sp).Set<InboxEnvelope>().ToListAsync();
        Assert.AreEqual(1, envelopes.Count);
        // Сохранилась первая доставка: дубль игнорируется целиком, а не перезаписывает тело.
        Assert.IsTrue(envelopes[0].Content.Contains("first", StringComparison.Ordinal));
    }

    [Test]
    public async Task Enqueue_SameMessageIdDifferentConsumers_BothAccepted()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();

        var first = await inboxService.EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("message-1", "consumer-1"));
        var second = await inboxService.EnqueueAsync(
            new TestInboxCommand(), new InboxMessageIdentity("message-1", "consumer-2"));

        // Одно и то же сообщение легитимно обрабатывается разными потребителями, поэтому
        // ключ дедупликации составной, а не голый MessageId.
        Assert.AreEqual(InboxEnqueueStatus.Accepted, first);
        Assert.AreEqual(InboxEnqueueStatus.Accepted, second);
        Assert.AreEqual(2, await GetDb(sp).Set<InboxEnvelope>().CountAsync());
    }

    [Test]
    public async Task Process_MessageWithoutRegisteredHandler_IsNotFetched()
    {
        // Обработчик не зарегистрирован: сообщение принято, но эта служба его не забирает.
        var sp = InitInboxServiceCollection().BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        await inboxService.EnqueueAsync(new TestInboxCommand(), new InboxMessageIdentity("message-1", "consumer-1"));

        var handler = sp.GetRequiredService<IInboxHandler>();
        var processed = await handler.ProcessAsync(CancellationToken.None);

        Assert.AreEqual(0, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.New, envelope.Status);
    }
}