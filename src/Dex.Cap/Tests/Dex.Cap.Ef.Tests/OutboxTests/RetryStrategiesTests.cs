using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.OutboxTests.RetryStrategies;
using Dex.Cap.Outbox.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Jobs;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Dex.Cap.Outbox.RetryStrategies;
using Dex.Extensions;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

public class RetryStrategiesTests : BaseTest
{
    private OutboxRetryStrategyConfigurator _configurator;
    private OutboxLockedJob _outboxLockedJob;

    [SetUp]
    public void SetUp()
    {
        _configurator = new OutboxRetryStrategyConfigurator();
        var envelop = new OutboxEnvelope(Guid.NewGuid(), Guid.NewGuid(), "Test", "SomeContent", DateTime.UtcNow);
        _outboxLockedJob = new OutboxLockedJob(envelop);
        ConfigureStrategy(5, "IncrementalRescheduling", _configurator);
    }

    [Test]
    [TestCase(100, 1, 1, OutboxMessageStatus.Succeeded, "IncrementalRescheduling")]
    [TestCase(1000, 0, 2, OutboxMessageStatus.Failed, "IncrementalRescheduling")]
    [TestCase(100, 1, 2, OutboxMessageStatus.Succeeded, "Incremental")]
    [TestCase(1000, 0, 2, OutboxMessageStatus.Failed, "Incremental")]
    [TestCase(100, 1, 2, OutboxMessageStatus.Succeeded, "Exponential")]
    [TestCase(1000, 0, 2, OutboxMessageStatus.Failed, "Exponential")]
    public async Task IncrementalRetry_ProcessMessage(int intervalMs, int expectedCount, int maxCount,
        OutboxMessageStatus expectedStatus, string retryStrategyName)
    {
        var serviceProvider = InitServiceCollection(strategyConfigure: strategyConfigurator =>
                ConfigureStrategy(intervalMs, retryStrategyName, strategyConfigurator))
            .AddScoped<IOutboxMessageHandler<TestErrorOutboxCommand>, TestErrorCommandHandler>()
            .BuildServiceProvider();

        TestErrorCommandHandler.Reset();

        var outboxService = serviceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
        var correlationId = Guid.NewGuid();
        await outboxService.EnqueueAsync(correlationId, new TestErrorOutboxCommand { MaxCount = maxCount });
        await SaveChanges(serviceProvider);

        var count = 0;
        TestErrorCommandHandler.OnProcess += (_, _) => { count++; };
        var handler = serviceProvider.GetRequiredService<IOutboxHandler>();
        var repeat = 5;
        while (repeat-- > 0)
        {
            await handler.ProcessAsync(CancellationToken.None);
            await Task.Delay(100);
        }

        // check

        using var scope = serviceProvider.CreateScope();
        var envelope = await GetDb(scope.ServiceProvider).Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == correlationId);
        Assert.AreEqual(expectedStatus, envelope.Status);
        Assert.AreEqual(expectedCount, count);
    }

    [Test]
    [TestCase(3, 0)]
    [TestCase(2, 2)]
    [TestCase(1, 1)]
    public void ResetRetriesIfNeeded_Success(int currentRetry, int expectedRetry)
    {
        var strategy = _configurator.RetryStrategy;

        _outboxLockedJob.Envelope.Retries = currentRetry;
        strategy.ResetRetriesIfNeeded(3, _outboxLockedJob);

        Assert.AreEqual(expectedRetry, _outboxLockedJob.Envelope.Retries);
    }

    [Test]
    [TestCase("2024-02-07T12:00:00Z", 3, "2024-02-07T12:05:00Z")]
    [TestCase("2024-02-07T12:00:00Z", 2, "2024-02-07T12:00:05Z")]
    public void CalculateNextStartDate_Success(string startDateStr, int currentRetry, string expectedDateStr)
    {
        var strategy = _configurator.RetryStrategy;

        var startDate = DateTime.Parse(startDateStr).ToUniversalTime();
        var options = new OutboxRetryStrategyOptions(startDate, currentRetry);
        var expectedDate = DateTime.Parse(expectedDateStr).ToUniversalTime();

        var result = strategy.CalculateNextStartDate(options, 3);

        Assert.AreEqual(expectedDate, result);
    }

    [Test]
    [TestCase(null, 3)]
    [TestCase("2024-02-07T12:00:00Z", null)]
    public void CalculateNextStartDate_ThrowArgumentNullException(string? startDateStr, int? currentRetry)
    {
        var strategy = _configurator.RetryStrategy;

        OutboxRetryStrategyOptions? options = startDateStr != null && currentRetry.HasValue
            ? new OutboxRetryStrategyOptions(DateTime.Parse(startDateStr).ToUniversalTime(), currentRetry.Value)
            : null;

        Assert.Throws<ArgumentNullException>(() => strategy.CalculateNextStartDate(options, 3));
    }

    private static void ConfigureStrategy(int interval, string retryStrategyName,
        OutboxRetryStrategyConfigurator configurator)
    {
        switch (retryStrategyName)
        {
            case "Incremental":
                configurator.UseOutboxIncrementalRetryStrategy(interval.MilliSeconds());
                break;
            case "Exponential":
                configurator.UseOutboxExponentialRetryStrategy(interval.MilliSeconds());
                break;
            case "IncrementalRescheduling":
                configurator.UseOutboxReSchedulingIncrementalRetryStrategy(interval.Seconds(),
                    interval.Minutes());
                break;
        }
    }
}