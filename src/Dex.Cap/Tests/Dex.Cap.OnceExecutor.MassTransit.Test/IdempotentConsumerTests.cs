using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.OnceExecutor.MassTransit;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dex.Cap.OnceExecutor.MassTransit.Test;

/// <summary>
/// Запись об ошибке в <see cref="IdempotentConsumer{TMessage,TDbContext}"/>.
/// </summary>
/// <remarks>
/// Ключ идемпотентности — единственное, чем запись связывается с записью once-executor и с
/// повторной доставкой того же сообщения, поэтому проверяется, что он попадает в запись и что
/// его вычисление не подменяет исходное исключение.
/// </remarks>
[TestFixture]
public class IdempotentConsumerTests
{
    [Test]
    public void Consume_WhenProcessFails_PutsIdempotentKeyIntoLogScope()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<KeyedMessage>(logger);

        ConsumeAndCatch(consumer, Context(new KeyedMessage { IdempotentKey = "order-42" }));

        Assert.That(logger.Records.Single().Scope["IdempotentKey"], Is.EqualTo("order-42"));
    }

    [Test]
    public void Consume_WhenMessageHasNoKey_FallsBackToMessageId()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<PlainMessage>(logger);
        var messageId = Guid.NewGuid();

        ConsumeAndCatch(consumer, Context(new PlainMessage(), messageId));

        Assert.That(logger.Records.Single().Scope["IdempotentKey"], Is.EqualTo(messageId.ToString("N")));
    }

    [Test]
    public void Consume_WhenKeyCannotBeComputed_LogsWithoutKeyAndKeepsOriginalException()
    {
        var logger = new RecordingLogger();
        var consumer = new FailingConsumer<PlainMessage>(logger);

        // ни ключа в сообщении, ни MessageId: вычисление ключа падает само, ещё до операции
        Func<Task> consume = () => consumer.Consume(Context(new PlainMessage()));
        var exception = Assert.ThrowsAsync<ArgumentNullException>(consume)!;

        var record = logger.Records.Single();
        Assert.That(record.Exception, Is.SameAs(exception));
        Assert.That(record.Scope["IdempotentKey"], Is.Null);
    }

    private static InvalidOperationException ConsumeAndCatch<TMessage>(IConsumer<TMessage> consumer, ConsumeContext<TMessage> context)
        where TMessage : class
    {
        Func<Task> consume = () => consumer.Consume(context);

        return Assert.ThrowsAsync<InvalidOperationException>(consume)!;
    }

    private static ConsumeContext<TMessage> Context<TMessage>(TMessage message, Guid? messageId = null)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(messageId);

        return context.Object;
    }

    /// <summary>
    /// Консьюмер, падающий до обращения к базе: тело операции не выполняется.
    /// </summary>
    private sealed class FailingConsumer<TMessage> : IdempotentConsumer<TMessage, object>
        where TMessage : class
    {
        public const string FailureMessage = "process failed";

        public FailingConsumer(ILogger logger) : base(OnceExecutor(), logger)
        {
        }

        protected override Task IdempotentProcess(ConsumeContext<TMessage> context) => Task.CompletedTask;

        private static IOnceExecutor<IEfTransactionOptions, object> OnceExecutor()
        {
            var executor = new Mock<IOnceExecutor<IEfTransactionOptions, object>>();
            executor
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<object, CancellationToken, Task>>(),
                    It.IsAny<IEfTransactionOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(FailureMessage));

            return executor.Object;
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<Dictionary<string, object?>> _scopes = [];

        public List<Record> Records { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scope = _scopes
                .SelectMany(x => x)
                .ToDictionary(x => x.Key, x => x.Value);

            Records.Add(new Record(logLevel, exception, scope));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            var scope = values.ToDictionary(x => x.Key, x => x.Value);

            _scopes.Add(scope);

            return new Scope(_scopes, scope);
        }

        internal sealed record Record(LogLevel Level, Exception? Exception, Dictionary<string, object?> Scope);

        private sealed class Scope(List<Dictionary<string, object?>> scopes, Dictionary<string, object?> scope) : IDisposable
        {
            public void Dispose() => scopes.Remove(scope);
        }
    }
}

/// <summary>
/// Сообщение со своим ключом идемпотентности.
/// </summary>
public sealed class KeyedMessage : IIdempotentKey
{
    public string IdempotentKey { get; init; } = string.Empty;
}

/// <summary>
/// Сообщение без ключа: ключом становится MessageId.
/// </summary>
public sealed class PlainMessage
{
    public string Name { get; init; } = string.Empty;
}