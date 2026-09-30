using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

public class OutboxContentLimitTests : BaseTest
{
    [Test]
    public async Task Enqueue_ContentOverLimit_ThrowsBeforeWrite()
    {
        const int limit = 64;
        await using var sp = InitServiceCollection()
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = limit)
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var oversized = new TestOutboxCommand { Args = new string('x', 1000) };

        var ex = NUnit.Framework.Assert.ThrowsAsync<OutboxContentTooLargeException>(
            (Func<Task>)(async () => await outboxService.EnqueueAsync(oversized)));

        Assert.AreEqual(limit, ex!.MaxContentLengthBytes);
        Assert.Greater(ex.ContentLengthBytes, limit);
        Assert.AreEqual(TestOutboxCommand.OutboxTypeId, ex.MessageType);

        // Проверка на постановке, поэтому строка в БД не появляется.
        await SaveChanges(sp);
        var count = await GetDb(sp).Set<OutboxEnvelope>().CountAsync();
        Assert.AreEqual(0, count);
    }

    [Test]
    public async Task Enqueue_ContentWithinLimit_IsStored()
    {
        await using var sp = InitServiceCollection()
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = OutboxOptions.DefaultMaxContentLengthBytes)
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var id = await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "small" });
        await SaveChanges(sp);

        Assert.AreNotEqual(Guid.Empty, id);
        var stored = await GetDb(sp).Set<OutboxEnvelope>().SingleAsync();
        Assert.AreEqual(id, stored.Id);
    }

    [Test]
    public async Task Enqueue_ContentExactlyAtLimit_IsAccepted_OneByteOver_IsRejected()
    {
        var message = new TestOutboxCommand { Args = "boundary" };

        int exactBytes;
        await using (var probe = InitServiceCollection().BuildServiceProvider())
        {
            var serialized = probe.GetRequiredService<IOutboxSerializer>().Serialize(message.GetType(), message);
            exactBytes = Encoding.UTF8.GetByteCount(serialized);
        }

        // Ровно предел проходит: проверка это `>`, а не `>=`, и строка реально сохраняется.
        await using var spAtLimit = InitServiceCollection()
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = exactBytes)
            .BuildServiceProvider();
        var id = await spAtLimit.GetRequiredService<IOutboxService>().EnqueueAsync(message);
        await SaveChanges(spAtLimit);
        Assert.AreNotEqual(Guid.Empty, id);
        var stored = await GetDb(spAtLimit).Set<OutboxEnvelope>().SingleAsync();
        Assert.AreEqual(id, stored.Id);

        // На один байт меньше предела - отказ, с точным размером в исключении.
        await using var spOverByOne = InitServiceCollection()
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = exactBytes - 1)
            .BuildServiceProvider();
        var overByOneService = spOverByOne.GetRequiredService<IOutboxService>();
        var ex = NUnit.Framework.Assert.ThrowsAsync<OutboxContentTooLargeException>(
            (Func<Task>)(async () => await overByOneService.EnqueueAsync(message)));
        Assert.AreEqual(exactBytes, ex!.ContentLengthBytes);
        Assert.AreEqual(exactBytes - 1, ex.MaxContentLengthBytes);
    }

    [Test]
    public async Task Enqueue_MultibyteContent_IsMeasuredInUtf8BytesNotChars()
    {
        // Дефолтный сериализатор экранирует не-ASCII в \uXXXX, поэтому его вывод всегда ASCII и байт == символов.
        // Различие байт/символы проявляется только на сериализаторе с сырым UTF-8 (relaxed escaping), какой
        // потребитель вправе подставить через IOutboxSerializer. На нём и проверяем, что предел меряется в БАЙТАХ.
        var message = new TestOutboxCommand { Args = new string('я', 200) };

        string content;
        await using (var probe = InitServiceCollection()
                         .AddScoped<IOutboxSerializer>(_ => new RawUtf8OutboxSerializer())
                         .BuildServiceProvider())
        {
            content = probe.GetRequiredService<IOutboxSerializer>().Serialize(message.GetType(), message);
        }
        var byteLen = Encoding.UTF8.GetByteCount(content);
        var charLen = content.Length;
        Assert.Greater(byteLen, charLen); // сырой UTF-8: байт больше, чем символов

        // Предел равен числу СИМВОЛОВ. Байтовая проверка отвергает (байт больше символов);
        // проверка по string.Length пропустила бы это тело.
        await using var sp = InitServiceCollection()
            .AddScoped<IOutboxSerializer>(_ => new RawUtf8OutboxSerializer())
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = charLen)
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();
        var ex = NUnit.Framework.Assert.ThrowsAsync<OutboxContentTooLargeException>(
            (Func<Task>)(async () => await outboxService.EnqueueAsync(message)));
        Assert.AreEqual(byteLen, ex!.ContentLengthBytes);
        Assert.AreEqual(charLen, ex.MaxContentLengthBytes);
    }

    [Test]
    public void Enqueue_UnregisteredDiscriminator_ResolvesDiscriminatorBeforeSizeGuard()
    {
        // Дискриминатор проверяется в CreateEnvelop ДО байтовой проверки. Даже при предельном лимите в 1 байт
        // незарегистрированный тип падает DiscriminatorResolveException, а не OutboxContentTooLargeException.
        using var sp = InitServiceCollection()
            .Configure<OutboxOptions>(o => o.MaxContentLengthBytes = 1)
            .BuildServiceProvider();

        var outboxService = sp.GetRequiredService<IOutboxService>();

        // ThrowsAsync, а не CatchAsync: нужен ровно этот тип. Его одного достаточно как доказательства
        // порядка guard-ов, потому что OutboxContentTooLargeException наследует OutboxException и под
        // DiscriminatorResolveException не подходит. Текст сообщения не проверяем: он русский и упал бы при
        // переводе рантайм-сообщений, а дискриминатор у TestEmptyMessage пуст, сверяться не с чем.
        NUnit.Framework.Assert.ThrowsAsync<DiscriminatorResolveException>(
            (Func<Task>)(async () => await outboxService.EnqueueAsync(new TestEmptyMessage())));
    }

    /// <summary>Сериализатор без экранирования не-ASCII: тело уходит сырым UTF-8, где байт больше символов.</summary>
    private sealed class RawUtf8OutboxSerializer : IOutboxSerializer
    {
        private readonly JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        public string Serialize<T>(T message) => JsonSerializer.Serialize(message, _options);
        public string Serialize(Type type, object message) => JsonSerializer.Serialize(message, type, _options);
        public T? Deserialize<T>(string message) => JsonSerializer.Deserialize<T>(message, _options);
        public object? Deserialize(Type type, string message) => JsonSerializer.Deserialize(message, type, _options);
    }
}