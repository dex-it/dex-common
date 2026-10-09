using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Dex.MassTransit.Rabbit.Tests;

/// <summary>
/// Маскировщик тела из контейнера консьюмера на настоящем конвейере MassTransit.
/// </summary>
/// <remarks>
/// <see cref="BaseConsumerTests"/> подставляет контекст двойником, а находится маскировщик через
/// <see cref="IServiceProvider"/>, который кладёт в контекст сам MassTransit. Эта опора — чужой
/// контракт, и проверяется она только здесь.
/// </remarks>
[TestFixture]
public class ConsumerContainerMaskingTests
{
    [Test]
    public async Task Consume_WhenMaskerRegisteredInContainer_WritesMaskedBody()
    {
        var logger = await ConsumeFailing(services => services.AddSingleton<IMessageDataMasker, ReplacingMasker>());

        Assert.That(logger.MessageData, Is.EqualTo(ReplacingMasker.Output));
    }

    [Test]
    public async Task Consume_WhenMaskerRegisteredAsScoped_WritesMaskedBody()
    {
        var logger = await ConsumeFailing(services => services.AddScoped<IMessageDataMasker, ReplacingMasker>());

        Assert.That(logger.MessageData, Is.EqualTo(ReplacingMasker.Output));
    }

    [Test]
    public async Task Consume_WhenDefaultMaskerRegistered_HidesSecret()
    {
        var logger = await ConsumeFailing(services => services.AddSingleton<IMessageDataMasker, SensitiveNamesMessageDataMasker>());

        Assert.That(logger.MessageData, Is.EqualTo("""{"Secret":"***"}"""));
    }

    [Test]
    public async Task Consume_WhenContainerHasNoMasker_WritesPlainBody()
    {
        var logger = await ConsumeFailing(_ => { });

        Assert.That(logger.MessageData, Is.EqualTo("""{"Secret":"s3cr3t"}"""));
    }

    private static async Task<CapturingLogger> ConsumeFailing(Action<IServiceCollection> configure)
    {
        var logger = new CapturingLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<SecretConsumer>>(logger);
        configure(services);
        services.AddMassTransitTestHarness(x => x.AddConsumer<SecretConsumer>());

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new SecretMessage { Secret = "s3cr3t" });
            Assert.That(await harness.Consumed.Any<SecretMessage>(), Is.True);
        }
        finally
        {
            await harness.Stop();
        }

        return logger;
    }

    private sealed class ReplacingMasker : IMessageDataMasker
    {
        public const string Output = "masked";

        public string Mask(ReadOnlySpan<byte> json, bool isComplete, int limit) => Output;
    }

    private sealed class CapturingLogger : ILogger<SecretConsumer>
    {
        private readonly ConcurrentQueue<object?> _messageData = new();

        public object? MessageData => _messageData.TryPeek(out var value) ? value : null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
                return;

            foreach (var pair in values)
            {
                if (pair.Key == "MessageData")
                    _messageData.Enqueue(pair.Value);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}

/// <summary>
/// Сообщение с секретом в теле.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class SecretMessage
{
    public string Secret { get; init; } = string.Empty;
}

/// <summary>
/// Консьюмер, падающий на любом сообщении.
/// </summary>
[UsedImplicitly]
public sealed class SecretConsumer(ILogger<SecretConsumer> logger) : BaseConsumer<SecretMessage>(logger)
{
    protected override Task Process(ConsumeContext<SecretMessage> context) => throw new InvalidOperationException("process failed");
}