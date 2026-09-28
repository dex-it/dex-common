using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Dex.MassTransit.Rabbit.Tests;

/// <summary>
/// Запись об упавшем консьюмере в <see cref="BaseConsumer{TMessage}"/>.
/// </summary>
/// <remarks>
/// Тело сообщения ничем не ограничено сверху, поэтому проверяется не только текст записи:
/// значение должно быть одним скаляром (последовательность хранилище логов разворачивает
/// поэлементно), а сбой сериализации не должен подменять исходное исключение.
/// </remarks>
[TestFixture]
public class BaseConsumerTests
{
    [Test]
    public void Consume_WhenProcessFails_RethrowsOriginalExceptionAndLogsIt()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);

        var exception = ConsumeAndCatch(consumer, Context(new TestMessage()));

        var record = logger.Records.Single();
        Assert.That(exception.Message, Is.EqualTo(FailingConsumer<TestMessage>.FailureMessage));
        Assert.That(record.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(record.Exception, Is.SameAs(exception));
    }

    [Test]
    public void Consume_WhenProcessFails_WritesMessageDataAsSingleValue()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = "abc", Ids = [1, 2] }));

        var messageData = logger.Records.Single().Values["MessageData"];
        Assert.That(messageData, Is.TypeOf<string>());
        Assert.That(messageData, Is.EqualTo("""{"Name":"abc","Ids":[1,2]}"""));
    }

    [Test]
    public void Consume_WhenProcessFails_WritesMessageIdentity()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);
        var messageId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        ConsumeAndCatch(consumer, Context(new TestMessage(), messageId, conversationId));

        var values = logger.Records.Single().Values;
        Assert.That(values["MessageType"], Is.EqualTo(typeof(TestMessage).FullName));
        Assert.That(values["MessageId"], Is.EqualTo(messageId));
        Assert.That(values["ConversationId"], Is.EqualTo(conversationId));
        Assert.That(values["RetryAttempt"], Is.EqualTo(0));
    }

    [Test]
    public void Consume_WhenMessageExceedsLimit_TruncatesToLimitAndMarksIt()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger, messageDataLimit: 32);

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = new string('a', 500) }));

        var messageData = (string) logger.Records.Single().Values["MessageData"]!;
        Assert.That(messageData, Does.StartWith("""{"Name":"aaa"""));
        Assert.That(messageData, Does.EndWith("..."));
        Assert.That(messageData[..^3], Has.Length.EqualTo(32));
    }

    [Test]
    public void Consume_WhenLimitSplitsMultibyteChar_DropsIncompleteTail()
    {
        var logger = new RecordingLogger();

        // {"Name":" — девять однобайтовых символов, дальше кириллица по два байта на символ
        var consumer = new FailingConsumer<TestMessage>(logger, messageDataLimit: 10);

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = new string('я', 20) }));

        var messageData = (string) logger.Records.Single().Values["MessageData"]!;
        Assert.That(messageData, Is.EqualTo("""{"Name":"..."""));
        Assert.That(messageData, Does.Not.Contain("�"));
    }

    [Test]
    public void Consume_WhenMessageIsNotSerializable_KeepsOriginalExceptionAndReportsIt()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<BrokenMessage>(logger);

        var exception = ConsumeAndCatch(consumer, Context(new BrokenMessage()));

        var messageData = (string) logger.Records.Single().Values["MessageData"]!;
        Assert.That(exception.Message, Is.EqualTo(FailingConsumer<BrokenMessage>.FailureMessage));
        Assert.That(messageData, Does.StartWith("<not serialized:"));
    }

    /// <remarks>
    /// Путь без наследования: консьюмер на несколько типов сообщений зовёт расширение из своего
    /// catch и должен получить ту же запись, что и наследник <see cref="BaseConsumer{TMessage}"/>.
    /// </remarks>
    [Test]
    public void LogConsumeError_WhenCalledDirectly_WritesSameRecord()
    {
        var logger = new RecordingLogger();
        var messageId = Guid.NewGuid();

        logger.LogConsumeError(Context(new TestMessage { Name = "abc" }, messageId), new InvalidOperationException("boom"));

        var record = logger.Records.Single();
        Assert.That(record.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(record.Values["MessageType"], Is.EqualTo(typeof(TestMessage).FullName));
        Assert.That(record.Values["MessageId"], Is.EqualTo(messageId));
        Assert.That(record.Values["MessageData"], Is.EqualTo("""{"Name":"abc","Ids":[]}"""));
    }

    [Test]
    public void Consume_WhenMaskerRegistered_WritesMaskedBody()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);
        var masker = new RecordingMasker(output: "masked body");

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = "secret" }, services: Services(masker)));

        var call = masker.Calls.Single();
        Assert.That(logger.Records.Single().Values["MessageData"], Is.EqualTo("masked body"));
        Assert.That(call.Json, Is.EqualTo("""{"Name":"secret","Ids":[]}"""));
        Assert.That(call.IsComplete, Is.True);
        Assert.That(call.Limit, Is.EqualTo(ConsumerLoggerExtensions.DefaultMessageDataLimit));
    }

    /// <remarks>
    /// Вход маскировщика ограничен запасом над лимитом, поэтому граф сообщения целиком не обходится,
    /// какой бы маскировщик ни был подключён; метку усечения ставит маскировщик.
    /// </remarks>
    [Test]
    public void Consume_WhenMaskerRegisteredAndBodyExceedsInputReserve_PassesPrefix()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger, messageDataLimit: 32);
        var masker = new RecordingMasker(output: "masked prefix");

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = new string('a', 500) }, services: Services(masker)));

        var call = masker.Calls.Single();
        Assert.That(logger.Records.Single().Values["MessageData"], Is.EqualTo("masked prefix"));
        Assert.That(call.Json, Does.StartWith("""{"Name":"aaa"""));
        Assert.That(call.Json, Has.Length.EqualTo(32 * MessageDataFormatter.MaskerInputFactor));
        Assert.That(call.IsComplete, Is.False);
        Assert.That(call.Limit, Is.EqualTo(32));
    }

    /// <remarks>
    /// Лимит режет вывод маски, а не вход: замаскированный длинный токен освобождает место под следующие поля.
    /// </remarks>
    [Test]
    public void Consume_WhenMaskFreesSpace_FollowingFieldsFitIntoLimit()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TokenMessage>(logger, messageDataLimit: 60);
        var services = Services(new SensitiveNamesMessageDataMasker());

        ConsumeAndCatch(consumer, Context(new TokenMessage { Token = new string('x', 100), Tail = "end" }, services: services));

        Assert.That(logger.Records.Single().Values["MessageData"], Is.EqualTo("""{"Token":"***","Tail":"end"}"""));
    }

    [Test]
    public void Consume_WhenMaskerRegisteredAndLimitSplitsMultibyteChar_PassesWholeChars()
    {
        var logger = new RecordingLogger();

        // {"Name":" — девять однобайтовых символов, дальше кириллица по два байта на символ; вход маскировщика — 12 байт
        var consumer = new FailingConsumer<TestMessage>(logger, messageDataLimit: 12 / MessageDataFormatter.MaskerInputFactor);
        var masker = new RecordingMasker(output: "masked");

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = new string('я', 20) }, services: Services(masker)));

        Assert.That(masker.Calls.Single().Bytes, Is.EqualTo("""{"Name":"я"""u8.ToArray()));
    }

    /// <remarks>
    /// Маскировщик — чужой код в обработчике ошибки: его сбой не должен ни подменить исходное
    /// исключение, ни открыть тело, ради маски которого он подключён.
    /// </remarks>
    [Test]
    public void Consume_WhenMaskerThrows_KeepsOriginalExceptionAndHidesBody()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);
        var masker = new RecordingMasker(failure: new FormatException("masker is broken"));

        var exception = ConsumeAndCatch(consumer, Context(new TestMessage { Name = "secret" }, services: Services(masker)));

        var record = logger.Records.Single();
        Assert.That(record.Exception, Is.SameAs(exception));
        Assert.That(record.Values["MessageData"], Is.EqualTo("<not masked: FormatException>"));
    }

    [Test]
    public void Consume_WhenMaskerCannotBeResolved_KeepsOriginalExceptionAndHidesBody()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);
        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(IMessageDataMasker))).Throws(new ObjectDisposedException("scope"));

        var exception = ConsumeAndCatch(consumer, Context(new TestMessage { Name = "secret" }, services: services.Object));

        var record = logger.Records.Single();
        Assert.That(record.Exception, Is.SameAs(exception));
        Assert.That(record.Values["MessageData"], Is.EqualTo("<not masked: ObjectDisposedException>"));
    }

    [Test]
    public void Consume_WhenContainerHasNoMasker_WritesPlainBody()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<TestMessage>(logger);

        ConsumeAndCatch(consumer, Context(new TestMessage { Name = "abc" }, services: Services(masker: null)));

        Assert.That(logger.Records.Single().Values["MessageData"], Is.EqualTo("""{"Name":"abc","Ids":[]}"""));
    }

    private static InvalidOperationException ConsumeAndCatch<TMessage>(BaseConsumer<TMessage> consumer, ConsumeContext<TMessage> context)
        where TMessage : class
    {
        var consume = () => consumer.Consume(context);

        return Assert.ThrowsAsync<InvalidOperationException>(consume)!;
    }

    private static ConsumeContext<TMessage> Context<TMessage>(TMessage message, Guid? messageId = null, Guid? conversationId = null,
        IServiceProvider? services = null)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(messageId);
        context.SetupGet(x => x.ConversationId).Returns(conversationId);
        context.Setup(x => x.TryGetPayload(out services)).Returns(services != null);

        return context.Object;
    }

    private static IServiceProvider Services(IMessageDataMasker? masker)
    {
        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(IMessageDataMasker))).Returns(masker);

        return services.Object;
    }

    private sealed class RecordingMasker(string output = "", Exception? failure = null) : IMessageDataMasker
    {
        public List<Call> Calls { get; } = [];

        public string Mask(ReadOnlySpan<byte> json, bool isComplete, int limit)
        {
            Calls.Add(new Call(json.ToArray(), isComplete, limit));

            return failure == null ? output : throw failure;
        }

        internal sealed record Call(byte[] Bytes, bool IsComplete, int Limit)
        {
            public string Json => Encoding.UTF8.GetString(Bytes);
        }
    }

    private sealed class FailingConsumer<TMessage>(ILogger logger, int? messageDataLimit = null) : BaseConsumer<TMessage>(logger)
        where TMessage : class
    {
        public const string FailureMessage = "process failed";

        protected override int MessageDataLimit => messageDataLimit ?? base.MessageDataLimit;

        protected override Task Process(ConsumeContext<TMessage> context) => throw new InvalidOperationException(FailureMessage);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<Record> Records { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];

            Records.Add(new Record(logLevel, exception, values.ToDictionary(x => x.Key, x => x.Value)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

        internal sealed record Record(LogLevel Level, Exception? Exception, Dictionary<string, object?> Values);

        private sealed class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

/// <summary>
/// Сообщение с коллекцией в теле.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class TestMessage
{
    public string Name { get; init; } = string.Empty;
    public int[] Ids { get; init; } = [];
}

/// <summary>
/// Сообщение с токеном и полем после него.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class TokenMessage
{
    public string Token { get; init; } = string.Empty;
    public string Tail { get; init; } = string.Empty;
}

/// <summary>
/// Сообщение, которое нельзя сериализовать.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class BrokenMessage
{
    public string Value => throw new NotSupportedException("getter is broken");
}