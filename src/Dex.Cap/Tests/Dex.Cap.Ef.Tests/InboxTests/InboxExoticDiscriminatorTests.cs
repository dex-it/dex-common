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
/// Дискриминатор уходит в SQL захвата параметром, поэтому его содержимое запрос не ломает.
/// Дискриминатор нельзя сменить существующему типу (он лежит в БД), поэтому отвергать рабочие
/// значения нельзя: onboarding такого контракта стал бы невозможен.
/// </summary>
public class InboxExoticDiscriminatorTests : BaseTest
{
    [Test]
    public async Task Process_MessageUrnDiscriminator_GoesThroughTheWholePath()
    {
        // 'urn:message:...' это конвенция MessageUrn у MassTransit, а этот репозиторий поставляет
        // Dex.MassTransit: значение из соседнего пакета обязано приниматься как есть.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<UrnDiscriminatorCommand>, UrnDiscriminatorCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new UrnDiscriminatorCommand { Args = "urn" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        Assert.AreEqual(1, processed, "an urn discriminator must survive the claim SQL");
        Assert.AreEqual(InboxMessageStatus.Succeeded, (await GetDb(sp).Set<InboxEnvelope>().SingleAsync()).Status);
    }

    [Test]
    public async Task Process_DiscriminatorWithQuoteAndBrace_GoesThroughTheWholePath()
    {
        // Кавычка сломала бы литерал на стороне Postgres, фигурная скобка - string.Format внутри EF.
        // Параметр снимает оба вопроса, поэтому даже такое значение обязано работать.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<HostileDiscriminatorCommand>, HostileDiscriminatorCommandHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new HostileDiscriminatorCommand { Args = "hostile" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        var processed = await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        Assert.AreEqual(1, processed, "a quote and a brace must not break the claim SQL");
        Assert.AreEqual(InboxMessageStatus.Succeeded, (await GetDb(sp).Set<InboxEnvelope>().SingleAsync()).Status);
    }

    [Test]
    public async Task Process_ExoticDiscriminator_StillDoesNotTakeForeignMessages()
    {
        // Параметризация не должна ослабить фильтр: чужое сообщение по-прежнему не наше.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<UrnDiscriminatorCommand>, UrnDiscriminatorCommandHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new UrnDiscriminatorCommand { Args = "mine" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "not mine" }, new InboxMessageIdentity("m-2", "c-1"));

        Assert.AreEqual(1, await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None));

        var foreign = await GetDb(sp).Set<InboxEnvelope>().SingleAsync(x => x.MessageId == "m-2");
        Assert.AreEqual(InboxMessageStatus.New, foreign.Status);
    }
}
