using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Dex.Cap.Inbox.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

public class InboxContentLimitTests : BaseTest
{
    [Test]
    public async Task Enqueue_ContentOverLimit_ThrowsBeforeInsert()
    {
        const int limit = 64;
        await using var sp = InitInboxServiceCollection()
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = limit)
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var oversized = new TestInboxCommand { Args = new string('x', 1000) };
        var identity = new InboxMessageIdentity("message-1", "consumer-1");

        var ex = NUnit.Framework.Assert.ThrowsAsync<InboxContentTooLargeException>(
            (Func<Task>)(async () => await inboxService.EnqueueAsync(oversized, identity)));

        Assert.AreEqual(limit, ex!.MaxContentLengthBytes);
        Assert.Greater(ex.ContentLengthBytes, limit);
        Assert.AreEqual(TestInboxCommand.InboxTypeId, ex.MessageType);

        // Проверка на приёме, до INSERT, поэтому строка не появляется.
        var count = await GetDb(sp).Set<InboxEnvelope>().CountAsync();
        Assert.AreEqual(0, count);
    }

    [Test]
    public async Task Enqueue_ContentWithinLimit_IsAccepted()
    {
        await using var sp = InitInboxServiceCollection()
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = InboxOptions.DefaultMaxContentLengthBytes)
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var status = await inboxService.EnqueueAsync(
            new TestInboxCommand { Args = "small" },
            new InboxMessageIdentity("message-1", "consumer-1"));

        Assert.AreEqual(InboxEnqueueStatus.Accepted, status);

        var stored = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual("message-1", stored.MessageId);
        Assert.AreEqual("consumer-1", stored.ConsumerId);
    }

    [Test]
    public async Task Enqueue_ContentExactlyAtLimit_IsAccepted_OneByteOver_IsRejected()
    {
        var message = new TestInboxCommand { Args = "boundary" };

        int exactBytes;
        await using (var probe = InitInboxServiceCollection().BuildServiceProvider())
        {
            var serialized = probe.GetRequiredService<IInboxSerializer>().Serialize(typeof(TestInboxCommand), message);
            exactBytes = Encoding.UTF8.GetByteCount(serialized);
        }

        // Ровно предел проходит: проверка это `>`, а не `>=`.
        await using var spAtLimit = InitInboxServiceCollection()
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = exactBytes)
            .BuildServiceProvider();
        var status = await spAtLimit.GetRequiredService<IInboxService>().EnqueueAsync(
            message, new InboxMessageIdentity("boundary-ok", "consumer-1"));
        Assert.AreEqual(InboxEnqueueStatus.Accepted, status);

        // На один байт меньше предела - отказ, с точным размером в исключении.
        await using var spOverByOne = InitInboxServiceCollection()
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = exactBytes - 1)
            .BuildServiceProvider();
        var overByOneService = spOverByOne.GetRequiredService<IInboxService>();
        var ex = NUnit.Framework.Assert.ThrowsAsync<InboxContentTooLargeException>(
            (Func<Task>)(async () => await overByOneService.EnqueueAsync(
                message, new InboxMessageIdentity("boundary-over", "consumer-1"))));
        Assert.AreEqual(exactBytes, ex!.ContentLengthBytes);
        Assert.AreEqual(exactBytes - 1, ex.MaxContentLengthBytes);
    }

    [Test]
    public async Task Enqueue_MultibyteContent_IsMeasuredInUtf8BytesNotChars()
    {
        // Дефолтный сериализатор экранирует не-ASCII в \uXXXX, поэтому его вывод всегда ASCII и байт == символов.
        // Различие байт/символы проявляется только на сериализаторе с сырым UTF-8 (relaxed escaping), какой
        // потребитель вправе подставить через IInboxSerializer. На нём и проверяем, что предел меряется в БАЙТАХ.
        var message = new TestInboxCommand { Args = new string('я', 200) };

        string content;
        await using (var probe = InitInboxServiceCollection()
                         .AddScoped<IInboxSerializer>(_ => new RawUtf8InboxSerializer())
                         .BuildServiceProvider())
        {
            content = probe.GetRequiredService<IInboxSerializer>().Serialize(typeof(TestInboxCommand), message);
        }
        var byteLen = Encoding.UTF8.GetByteCount(content);
        var charLen = content.Length;
        Assert.Greater(byteLen, charLen); // сырой UTF-8: байт больше, чем символов

        // Предел равен числу СИМВОЛОВ. Байтовая проверка отвергает (байт больше символов);
        // проверка по string.Length пропустила бы это тело.
        await using var sp = InitInboxServiceCollection()
            .AddScoped<IInboxSerializer>(_ => new RawUtf8InboxSerializer())
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = charLen)
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var ex = NUnit.Framework.Assert.ThrowsAsync<InboxContentTooLargeException>(
            (Func<Task>)(async () => await inboxService.EnqueueAsync(
                message, new InboxMessageIdentity("message-1", "consumer-1"))));
        Assert.AreEqual(byteLen, ex!.ContentLengthBytes);
        Assert.AreEqual(charLen, ex.MaxContentLengthBytes);
    }

    [Test]
    public void Enqueue_DefaultIdentity_ValidatesIdentityBeforeSizeGuard()
    {
        // Идентичность проверяется в EnsureInitialized ДО байтовой проверки. Даже при предельном лимите в 1 байт
        // и заведомо большом теле незаполненная identity падает ArgumentException("identity"), а не
        // InboxContentTooLargeException: размер не меряется прежде валидации входа.
        using var sp = InitInboxServiceCollection()
            .Configure<InboxOptions>(o => o.MaxContentLengthBytes = 1)
            .BuildServiceProvider();

        var inboxService = sp.GetRequiredService<IInboxService>();
        var oversized = new TestInboxCommand { Args = new string('x', 1000) };

        var ex = NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(
            (Func<Task>)(async () => await inboxService.EnqueueAsync(oversized, default)));
        Assert.AreEqual("identity", ex!.ParamName);
    }

    /// <summary>Сериализатор без экранирования не-ASCII: тело уходит сырым UTF-8, где байт больше символов.</summary>
    private sealed class RawUtf8InboxSerializer : IInboxSerializer
    {
        private readonly JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        public string Serialize(Type type, object obj) => JsonSerializer.Serialize(obj, type, _options);
        public object? Deserialize(Type type, string input) => JsonSerializer.Deserialize(input, type, _options);
    }
}