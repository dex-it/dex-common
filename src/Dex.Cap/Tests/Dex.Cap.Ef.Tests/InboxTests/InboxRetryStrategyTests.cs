using System;
using Dex.Cap.Inbox.Options;
using Dex.Cap.Inbox.RetryStrategies;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Ретрай-стратегии считают момент следующей попытки. Единственная нетривиальная арифметика модуля,
/// и она публичный API: InboxRetryStrategyConfigurator.UseIncrementalStrategy/UseExponentialStrategy.
/// </summary>
public class InboxRetryStrategyTests
{
    private static readonly DateTime Anchor = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Default_ReturnsStartDateAsIs_MeaningNoExtraDelay()
    {
        var strategy = new InboxRetryStrategyConfigurator().RetryStrategy;

        var next = strategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 1));

        Assert.AreEqual(Anchor, next);
    }

    [Test]
    public void Incremental_ShiftsByFixedInterval_RegardlessOfRetryNumber()
    {
        var configurator = new InboxRetryStrategyConfigurator();
        configurator.UseIncrementalStrategy(TimeSpan.FromSeconds(30));

        Assert.AreEqual(Anchor.AddSeconds(30), configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 1)));
        Assert.AreEqual(Anchor.AddSeconds(30), configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 5)));
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 4)]
    [TestCase(4, 8)]
    public void Exponential_DoublesWithEveryRetry(int retry, int expectedSeconds)
    {
        var configurator = new InboxRetryStrategyConfigurator();
        configurator.UseExponentialStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5));

        var next = configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, retry));

        Assert.AreEqual(Anchor.AddSeconds(expectedSeconds), next);
    }

    [Test]
    public void Exponential_IsCappedByMaxDelay()
    {
        var configurator = new InboxRetryStrategyConfigurator();
        configurator.UseExponentialStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

        // 2^9 = 512s, потолок обязан удержать 10s.
        var next = configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 10));

        Assert.AreEqual(Anchor.AddSeconds(10), next);
    }

    [Test]
    public void Exponential_HugeRetryNumber_DoesNotOverflowAndStaysCapped()
    {
        var configurator = new InboxRetryStrategyConfigurator();
        configurator.UseExponentialStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5));

        // Math.Pow(2, 5000) = +Infinity: множитель обязан упереться в потолок, а не переполнить тики.
        var next = configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 5001));

        Assert.AreEqual(Anchor.AddMinutes(5), next);
    }

    [Test]
    public void Exponential_RetryBelowOne_IsTreatedAsFirstRetry()
    {
        var configurator = new InboxRetryStrategyConfigurator();
        configurator.UseExponentialStrategy(TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(5));

        Assert.AreEqual(Anchor.AddSeconds(2), configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, 0)));
        Assert.AreEqual(Anchor.AddSeconds(2), configurator.RetryStrategy.CalculateNextStartDate(new InboxRetryStrategyOptions(Anchor, null)));
    }

    [Test]
    public void Configurator_RejectsInvalidArguments()
    {
        var configurator = new InboxRetryStrategyConfigurator();

        NUnit.Framework.Assert.Throws<ArgumentOutOfRangeException>((Action)(() => configurator.UseIncrementalStrategy(TimeSpan.Zero)));
        NUnit.Framework.Assert.Throws<ArgumentOutOfRangeException>((Action)(() => configurator.UseExponentialStrategy(TimeSpan.Zero, TimeSpan.FromMinutes(1))));

        // maxDelay ниже baseDelay сделал бы потолок бессмысленным: первая же попытка его пробивает.
        NUnit.Framework.Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            configurator.UseExponentialStrategy(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1))));

        NUnit.Framework.Assert.Throws<ArgumentNullException>((Action)(() => configurator.RetryStrategy = null!));
    }
}
