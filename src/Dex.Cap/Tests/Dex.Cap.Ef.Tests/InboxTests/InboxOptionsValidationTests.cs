using System;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Правила валидаторов опций и арифметика стартовой задержки. Базы данных здесь не нужно,
/// поэтому фикстура не наследует <see cref="BaseTest"/>. Применение правил на старте хоста
/// проверяет <see cref="InboxOptionsStartupTests"/>.
/// </summary>
[TestFixture]
public class InboxOptionsValidationTests
{
    [Test]
    public void Options_Defaults_AreValid()
    {
        var result = new InboxOptionsValidator().Validate(null, new InboxOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [TestCase(0, 100, 1, "Retries")]
    [TestCase(-1, 100, 1, "Retries")]
    [TestCase(3, 0, 1, "MessagesToProcess")]
    [TestCase(3, 100, 0, "ConcurrencyLimit")]
    [TestCase(3, 10, 11, "should not exceed")]
    public void Options_InvalidCombination_IsRejected(int retries, int messagesToProcess, int concurrencyLimit, string expectedInMessage)
    {
        var options = new InboxOptions
        {
            Retries = retries,
            MessagesToProcess = messagesToProcess,
            ConcurrencyLimit = concurrencyLimit
        };

        var result = new InboxOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(expectedInMessage, StringComparison.Ordinal), result.FailureMessage);
    }

    [Test]
    public void Options_NonPositiveGetFreeMessagesTimeout_IsRejected()
    {
        var result = new InboxOptionsValidator().Validate(null, new InboxOptions { GetFreeMessagesTimeout = TimeSpan.Zero });

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(InboxOptions.GetFreeMessagesTimeout), StringComparison.Ordinal));
    }

    [Test]
    public void HandlerOptions_Defaults_AreValid()
    {
        var result = new InboxHandlerOptionsValidator().Validate(null, new InboxHandlerOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [Test]
    public void HandlerOptions_InvertedInitDelayRange_IsRejected()
    {
        var options = new InboxHandlerOptions
        {
            HandlerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(30), Max = TimeSpan.FromSeconds(5) }
        };

        var result = new InboxHandlerOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(InboxHandlerOptions.HandlerInitDelay), StringComparison.Ordinal));
    }

    [Test]
    public void HandlerOptions_NonPositivePeriod_IsRejected()
    {
        var result = new InboxHandlerOptionsValidator().Validate(null, new InboxHandlerOptions { Period = TimeSpan.Zero });

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(InboxHandlerOptions.Period), StringComparison.Ordinal));
    }

    [Test]
    public void InitDelayRange_FixedRange_ReturnsExactValue()
    {
        var range = new InitDelayRange { Min = TimeSpan.FromSeconds(7), Max = TimeSpan.FromSeconds(7) };

        Assert.AreEqual(TimeSpan.FromSeconds(7), range.GetDelay());
    }

    [Test]
    public void InitDelayRange_ZeroRange_MeansNoDelay()
    {
        var range = new InitDelayRange { Min = TimeSpan.Zero, Max = TimeSpan.Zero };

        Assert.AreEqual(TimeSpan.Zero, range.GetDelay());
    }

    [Test]
    public void InitDelayRange_SubMillisecondRange_DoesNotThrow()
    {
        // Min < Max, но после каста в миллисекунды они равны: RandomNumberGenerator.GetInt32
        // требует fromInclusive < toExclusive и упал бы.
        var range = new InitDelayRange { Min = TimeSpan.FromTicks(1), Max = TimeSpan.FromTicks(2) };

        Assert.AreEqual(TimeSpan.FromTicks(1), range.GetDelay());
    }

    [Test]
    public void InitDelayRange_NormalRange_StaysWithinBounds()
    {
        var range = new InitDelayRange { Min = TimeSpan.FromSeconds(5), Max = TimeSpan.FromSeconds(15) };

        for (var i = 0; i < 50; i++)
        {
            var delay = range.GetDelay();
            Assert.GreaterOrEqual(delay, TimeSpan.FromSeconds(5));
            Assert.Less(delay, TimeSpan.FromSeconds(15));
        }
    }
}
