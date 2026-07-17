using System;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Таймаут команды задаётся целыми секундами, поэтому доля секунды усекается в ноль, а ноль означает
    /// «таймаут не задан»: настройка молча потерялась бы целиком, а захват партии ушёл бы с дефолтом провайдера,
    /// то есть с временем БОЛЬШИМ запрошенного. Валидатор обязан отвергать такое значение на старте.
    /// </summary>
    [TestCase(0)]
    [TestCase(-1000)]
    [TestCase(500)]
    [TestCase(999)]
    public void Options_GetFreeMessagesTimeoutBelowOneSecond_IsRejected(int milliseconds)
    {
        var options = new InboxOptions { GetFreeMessagesTimeout = TimeSpan.FromMilliseconds(milliseconds) };

        var result = new InboxOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(InboxOptions.GetFreeMessagesTimeout), StringComparison.Ordinal));
    }

    [Test]
    public void Options_GetFreeMessagesTimeoutOfExactlyOneSecond_IsAccepted()
    {
        var options = new InboxOptions { GetFreeMessagesTimeout = TimeSpan.FromSeconds(1) };

        var result = new InboxOptionsValidator().Validate(null, options);

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
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

    /// <summary>
    /// Величины, уходящие в <see cref="Task.Delay(TimeSpan, System.Threading.CancellationToken)"/>, обязаны
    /// отвергаться на старте, а не ронять хост позже в фоне.
    /// </summary>
    /// <remarks>
    /// Обе фоновые службы сначала выжидают свой InitDelay, поэтому без проверки хост успевал бы подняться
    /// «здоровым», а падал бы уже фоном, где причину пришлось бы искать по логам.
    /// </remarks>
    [Test]
    public void HandlerOptions_DelayBeyondTheTimerLimit_IsRejected(
        [Values(nameof(InboxHandlerOptions.Period), nameof(InboxHandlerOptions.CleanupInterval), nameof(InboxHandlerOptions.CleanupBatchDelay))]
        string propertyName)
    {
        // 49.71 суток это предел таймера платформы; всё, что больше, Task.Delay уже не принимает.
        var beyondTheLimit = TimeSpan.FromMilliseconds(uint.MaxValue - 1) + TimeSpan.FromSeconds(1);
        var options = new InboxHandlerOptions();

        switch (propertyName)
        {
            case nameof(InboxHandlerOptions.Period):
                options.Period = beyondTheLimit;
                break;
            case nameof(InboxHandlerOptions.CleanupInterval):
                options.CleanupInterval = beyondTheLimit;
                break;
            default:
                options.CleanupBatchDelay = beyondTheLimit;
                break;
        }

        // Прежде всего: значение действительно нерабочее, а не просто «некрасивое».
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => Task.Delay(beyondTheLimit, CancellationToken.None)));

        var result = new InboxHandlerOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded, $"{propertyName} beyond the timer limit must not pass validation");
        Assert.IsTrue(result.FailureMessage!.Contains(propertyName, StringComparison.Ordinal), result.FailureMessage);
    }

    /// <summary>
    /// Ретеншен это не ожидание: общий потолок задержки к нему не применяется.
    /// </summary>
    /// <remarks>
    /// <see cref="InboxHandlerOptions.CleanupOlderThan"/> уходит в <c>DateTime.Subtract</c>, а не в
    /// <c>Task.Delay</c>. Окно дедупликации в 60 суток законно и обязано проходить валидацию, хотя оно и
    /// длиннее предела таймера.
    /// </remarks>
    [Test]
    public void HandlerOptions_RetentionLongerThanTheTimerLimit_IsAccepted()
    {
        var options = new InboxHandlerOptions { CleanupOlderThan = TimeSpan.FromDays(60) };

        var result = new InboxHandlerOptionsValidator().Validate(null, options);

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
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