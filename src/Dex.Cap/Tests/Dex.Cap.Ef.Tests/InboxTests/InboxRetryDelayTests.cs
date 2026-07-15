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
/// Задержка повтора обязана отсчитываться от момента отказа. Отсчёт от расписания работает только пока
/// обработка за ним успевает: стоит ей отстать, и следующая дата оказывается в прошлом, то есть backoff
/// исчезает ровно там, где он и нужен.
/// </summary>
public class InboxRetryDelayTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        TestErrorInboxCommandHandler.ProcessAttempts = 0;
    }

    [Test]
    public async Task Failure_OfABackloggedMessage_StillGetsARealDelay()
    {
        var sp = InitInboxServiceCollection(
                retries: 3,
                strategyConfigure: c => c.UseIncrementalStrategy(TimeSpan.FromMinutes(10)))
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, TestErrorInboxCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestErrorInboxCommand { Args = "poison" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        // Сообщение отстало от расписания на час: ровно тот случай, где отсчёт от StartAtUtc давал
        // дату в прошлом и повтор без задержки.
        using (var ageScope = sp.CreateScope())
        {
            await ageScope.ServiceProvider.GetRequiredService<TestDbContext>()
                .Set<InboxEnvelope>()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.StartAtUtc, DateTime.UtcNow.AddHours(-1))
                    .SetProperty(x => x.ScheduledStartIndexing, DateTime.UtcNow.AddHours(-1)));
        }

        var before = DateTime.UtcNow;
        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);
        Assert.AreEqual(1, processed);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.Failed, envelope.Status);
        Assert.AreEqual(1, envelope.Retries);

        // Следующая попытка обязана уехать в будущее, а не остаться в прошлом.
        Assert.IsNotNull(envelope.StartAtUtc);
        Assert.Greater(envelope.StartAtUtc!.Value, before.AddMinutes(9), "retry must be delayed from the failure moment");

        // И сообщение не должно немедленно перевыбираться.
        Assert.AreEqual(0, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None),
            "a delayed message must not be fetched before its StartAtUtc");
        Assert.AreEqual(1, TestErrorInboxCommandHandler.ProcessAttempts);
    }
}
