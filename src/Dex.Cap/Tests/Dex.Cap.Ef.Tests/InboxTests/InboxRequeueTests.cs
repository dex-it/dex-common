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

/// <summary>
/// Программный возврат похороненных сообщений через <see cref="IInboxDeadLetterService"/>: замена ручного
/// SQL, который иначе тёк бы схемой и инвариантом сброса нескольких колонок наружу.
/// </summary>
public class InboxRequeueTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestErrorInboxCommandHandler.ProcessAttempts = 0;
    }

    [Test]
    public async Task Requeue_AfterCauseFixed_MessageIsProcessedSuccessfully()
    {
        var sp = InitInboxServiceCollection(retries: 1)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var identity = new InboxMessageIdentity("message-1", "consumer-1");

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(new TestInboxCommand { Args = "valid" }, identity);

        var validContent = (await GetDb(sp).Set<InboxEnvelope>().AsNoTracking().SingleAsync()).Content;

        // Ломаем тело так, как это делает несовместимое изменение схемы сообщения, и хороним его.
        await GetDb(sp).Set<InboxEnvelope>().ExecuteUpdateAsync(s => s.SetProperty(x => x.Content, "{ this is not json"));
        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        Assert.AreEqual(InboxMessageStatus.DeadLettered,
            (await GetDb(sp).Set<InboxEnvelope>().AsNoTracking().SingleAsync()).Status);

        // Причина устранена: тело снова валидно.
        await GetDb(sp).Set<InboxEnvelope>().ExecuteUpdateAsync(s => s.SetProperty(x => x.Content, validContent));

        var requeued = await sp.GetRequiredService<IInboxDeadLetterService>().RequeueAsync(identity);
        Assert.IsTrue(requeued, "a dead lettered message must be reported as returned to processing");

        // Возврат согласованно сбросил состояние отказа, иначе выборка не увидела бы строку.
        var afterRequeue = await GetDb(sp).Set<InboxEnvelope>().AsNoTracking().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.New, afterRequeue.Status);
        Assert.AreEqual(0, afterRequeue.Retries);
        Assert.IsNotNull(afterRequeue.ScheduledStartIndexing);
        Assert.IsNull(afterRequeue.LockId);

        Assert.AreEqual(1, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None),
            "the requeued message must be fetched again");

        var processed = await GetDb(sp).Set<InboxEnvelope>().AsNoTracking().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Succeeded, processed.Status);
        Assert.AreEqual(0, processed.Retries);
    }

    [Test]
    public async Task RequeueAll_ReturnsOnlyDeadLetteredOfOwnDiscriminators()
    {
        // Обработчик зарегистрирован только для TestInboxCommand, поэтому TestErrorInboxCommand это чужой
        // дискриминатор: его строки этот сервис возвращать не должен.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await InsertAsync(sp, "own-dead", TestInboxCommand.InboxTypeId, InboxMessageStatus.DeadLettered, retries: 5);
        await InsertAsync(sp, "own-new", TestInboxCommand.InboxTypeId, InboxMessageStatus.New);
        await InsertAsync(sp, "own-done", TestInboxCommand.InboxTypeId, InboxMessageStatus.Succeeded);
        await InsertAsync(sp, "foreign-dead", TestErrorInboxCommand.InboxTypeId, InboxMessageStatus.DeadLettered);

        var requeued = await sp.GetRequiredService<IInboxDeadLetterService>().RequeueAllAsync();
        Assert.AreEqual(1, requeued, "only the own dead lettered message must be returned");

        Assert.AreEqual(InboxMessageStatus.New, await StatusOfAsync(sp, "own-dead"));
        Assert.AreEqual(0, (await RowAsync(sp, "own-dead")).Retries);
        Assert.IsNotNull((await RowAsync(sp, "own-dead")).ScheduledStartIndexing);

        // Не-похороненные и чужие строки не тронуты.
        Assert.AreEqual(InboxMessageStatus.Succeeded, await StatusOfAsync(sp, "own-done"));
        Assert.AreEqual(InboxMessageStatus.DeadLettered, await StatusOfAsync(sp, "foreign-dead"));
        Assert.IsNull((await RowAsync(sp, "own-new")).Updated, "a New row must stay untouched");
    }

    [Test]
    public async Task Requeue_LiveOrForeignOrUnknownMessage_ReturnsFalseAndTouchesNothing()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        await InsertAsync(sp, "live", TestInboxCommand.InboxTypeId, InboxMessageStatus.New);
        await InsertAsync(sp, "foreign-dead", TestErrorInboxCommand.InboxTypeId, InboxMessageStatus.DeadLettered);

        var service = sp.GetRequiredService<IInboxDeadLetterService>();

        Assert.IsFalse(await service.RequeueAsync(new InboxMessageIdentity("live", "consumer-1")),
            "a message that is not dead lettered has nothing to requeue");
        Assert.IsFalse(await service.RequeueAsync(new InboxMessageIdentity("foreign-dead", "consumer-1")),
            "a dead lettered message of a foreign discriminator must not be requeued");
        Assert.IsFalse(await service.RequeueAsync(new InboxMessageIdentity("unknown", "consumer-1")),
            "an unknown message must not be requeued");

        Assert.AreEqual(InboxMessageStatus.New, await StatusOfAsync(sp, "live"));
        Assert.IsNull((await RowAsync(sp, "live")).Updated);
        Assert.AreEqual(InboxMessageStatus.DeadLettered, await StatusOfAsync(sp, "foreign-dead"));
    }

    private static async Task InsertAsync(
        IServiceProvider sp, string messageId, string messageType, InboxMessageStatus status, int retries = 0)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var envelope = new InboxEnvelope(Guid.NewGuid(), messageId, "consumer-1", messageType, "{}")
        {
            Status = status,
            Retries = retries
        };

        // Завершённые сообщения выведены из выборки: у них нет ScheduledStartIndexing, как в проде.
        if (status is InboxMessageStatus.DeadLettered or InboxMessageStatus.Succeeded)
        {
            envelope.ScheduledStartIndexing = null;
        }

        await db.AddAsync(envelope);
        await db.SaveChangesAsync();
    }

    private static async Task<InboxEnvelope> RowAsync(IServiceProvider sp, string messageId) =>
        await GetDb(sp).Set<InboxEnvelope>().AsNoTracking().SingleAsync(x => x.MessageId == messageId);

    private static async Task<InboxMessageStatus> StatusOfAsync(IServiceProvider sp, string messageId) =>
        (await RowAsync(sp, messageId)).Status;
}
