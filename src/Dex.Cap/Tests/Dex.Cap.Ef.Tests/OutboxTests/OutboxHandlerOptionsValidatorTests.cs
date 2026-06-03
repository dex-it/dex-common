using System;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

[TestFixture]
public class OutboxHandlerOptionsValidatorTests
{
    private static OutboxHandlerOptions ValidOptions() => new();

    [Test]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var result = new OutboxHandlerOptionsValidator().Validate(null, ValidOptions());

        NUnit.Framework.Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_MinGreaterThanMax_Fails()
    {
        var options = ValidOptions();
        options.HandlerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(10), Max = TimeSpan.FromSeconds(1) };

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("HandlerInitDelay.Min must be <= Max"));
    }

    [Test]
    public void Validate_NegativeMin_Fails()
    {
        var options = ValidOptions();
        options.CleanerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(-1), Max = TimeSpan.FromSeconds(5) };

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("CleanerInitDelay.Min must be >= 0"));
    }

    [Test]
    public void Validate_NullRange_Fails()
    {
        var options = ValidOptions();
        options.HandlerInitDelay = null!;

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("HandlerInitDelay must not be null"));
    }

    [Test]
    public void Validate_NegativeMax_Fails()
    {
        var options = ValidOptions();
        options.HandlerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(1), Max = TimeSpan.FromSeconds(-1) };

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("HandlerInitDelay.Max must be >= 0"));
    }

    [Test]
    public void Validate_NullOptions_Throws()
    {
        NUnit.Framework.Assert.Throws<ArgumentNullException>((Action)Act);
        return;

        void Act() => new OutboxHandlerOptionsValidator().Validate(null, null!);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_PeriodNotPositive_Fails(int seconds)
    {
        var options = ValidOptions();
        options.Period = TimeSpan.FromSeconds(seconds);

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("Period must be > 0"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_CleanupIntervalNotPositive_Fails(int seconds)
    {
        var options = ValidOptions();
        options.CleanupInterval = TimeSpan.FromSeconds(seconds);

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("CleanupInterval must be > 0"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_CleanupOlderThanNotPositive_Fails(int days)
    {
        var options = ValidOptions();
        options.CleanupOlderThan = TimeSpan.FromDays(days);

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("CleanupOlderThan must be > 0"));
    }

    [Test]
    public void Validate_InitDelayMaxExceedsOneHour_Fails()
    {
        var options = ValidOptions();
        options.HandlerInitDelay = new InitDelayRange { Min = TimeSpan.Zero, Max = TimeSpan.FromHours(2) };

        var result = new OutboxHandlerOptionsValidator().Validate(null, options);

        NUnit.Framework.Assert.That(result.Failed, Is.True);
        NUnit.Framework.Assert.That(result.FailureMessage, Does.Contain("HandlerInitDelay.Max must be <="));
    }
}