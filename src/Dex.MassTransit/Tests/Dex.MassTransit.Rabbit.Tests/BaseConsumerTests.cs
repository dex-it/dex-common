using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    private static InvalidOperationException ConsumeAndCatch<TMessage>(BaseConsumer<TMessage> consumer, ConsumeContext<TMessage> context)
        where TMessage : class
    {
        var consume = () => consumer.Consume(context);

        return Assert.ThrowsAsync<InvalidOperationException>(consume)!;
    }

    private static ConsumeContext<TMessage> Context<TMessage>(TMessage message, Guid? messageId = null, Guid? conversationId = null)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(messageId);
        context.SetupGet(x => x.ConversationId).Returns(conversationId);

        return context.Object;
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
public sealed class TestMessage
{
    public string Name { get; init; } = string.Empty;
    public int[] Ids { get; init; } = [];
}

/// <summary>
/// Сообщение, которое нельзя сериализовать.
/// </summary>
public sealed class BrokenMessage
{
    public string Value => throw new NotSupportedException("getter is broken");
}