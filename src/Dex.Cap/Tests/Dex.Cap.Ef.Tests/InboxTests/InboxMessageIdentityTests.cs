using System;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Идентичность сообщения проверяется конструктором, но <c>default</c> обходит его всегда: значение по
/// умолчанию есть у любой структуры. Значит, отвергать такую пару обязана граница ядра.
/// </summary>
public class InboxMessageIdentityTests : BaseTest
{
    [Test]
    public void Constructor_RejectsEmptyHalves(
        [Values(null, "", " ")] string? messageId)
    {
        NUnit.Framework.Assert.Catch<ArgumentException>((Action)(() => _ = new InboxMessageIdentity(messageId!, "consumer")));
        NUnit.Framework.Assert.Catch<ArgumentException>((Action)(() => _ = new InboxMessageIdentity("message", messageId!)));
    }

    /// <summary>
    /// Приём обязан назвать параметр, который передавал ВЫЗЫВАЮЩИЙ.
    /// </summary>
    /// <remarks>
    /// Без проверки на границе неинициализированная пара доезжала до конструктора конверта и падала там с
    /// именем внутреннего параметра ('messageId'), которого в сигнатуре приёма нет вовсе: там есть identity.
    /// </remarks>
    [Test]
    public void Enqueue_DefaultIdentity_IsRejectedNamingTheCallerParameter()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var exception = NUnit.Framework.Assert.CatchAsync<ArgumentException>((Func<Task>)(async () =>
            await sp.GetRequiredService<IInboxService>()
                .EnqueueAsync(new TestInboxCommand { Args = "x" }, default)));

        Assert.AreEqual("identity", exception!.ParamName,
            "the exception must name the parameter of the method the caller actually called");
    }

    /// <summary>
    /// Возврат из dead letter обязан упасть, а не отчитаться «возвращать нечего».
    /// </summary>
    /// <remarks>
    /// Тут молчание опаснее: предикат сравнялся бы с null, не нашёл бы ни одной строки, и оператор получил бы
    /// штатное false, неотличимое от «такого похороненного сообщения нет».
    /// </remarks>
    [Test]
    public void Requeue_DefaultIdentity_IsRejectedInsteadOfReportingNothingToRequeue()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var exception = NUnit.Framework.Assert.CatchAsync<ArgumentException>((Func<Task>)(async () =>
            await sp.GetRequiredService<IInboxDeadLetterService>().RequeueAsync(default)));

        Assert.AreEqual("identity", exception!.ParamName);
    }

    /// <summary>
    /// Проверка границы не должна задевать корректную пару.
    /// </summary>
    [Test]
    public async Task Requeue_InitializedIdentityOfAnUnknownMessage_StillReportsNothingToRequeue()
    {
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .BuildServiceProvider();

        var requeued = await sp.GetRequiredService<IInboxDeadLetterService>()
            .RequeueAsync(new InboxMessageIdentity("never-existed", "c-1"));

        Assert.IsFalse(requeued, "a well-formed identity of a message that does not exist is a normal 'nothing to do'");
    }
}