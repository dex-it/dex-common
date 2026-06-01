using System;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

[TestFixture]
public class InitDelayRangeTests
{
    [Test]
    public void GetDelay_WhenMinEqualsMax_ReturnsThatValue()
    {
        var range = new InitDelayRange { Min = TimeSpan.FromSeconds(7), Max = TimeSpan.FromSeconds(7) };

        NUnit.Framework.Assert.That(range.GetDelay(), Is.EqualTo(TimeSpan.FromSeconds(7)));
    }

    [Test]
    public void GetDelay_WhenZeroRange_ReturnsZero()
    {
        var range = new InitDelayRange { Min = TimeSpan.Zero, Max = TimeSpan.Zero };

        NUnit.Framework.Assert.That(range.GetDelay(), Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void GetDelay_WhenSubMillisecondRange_ReturnsMin()
    {
        // Min < Max, но после каста в ms оба = 0 — GetInt32(0, 0) бросил бы исключение без защиты.
        var min = TimeSpan.FromTicks(1000); // 0.1 ms
        var max = TimeSpan.FromTicks(5000); // 0.5 ms
        var range = new InitDelayRange { Min = min, Max = max };

        NUnit.Framework.Assert.That(range.GetDelay(), Is.EqualTo(min));
    }

    [Test]
    public void GetDelay_WhenMinLessThanMax_ReturnsValueInHalfOpenInterval()
    {
        var min = TimeSpan.FromSeconds(5);
        var max = TimeSpan.FromSeconds(15);
        var range = new InitDelayRange { Min = min, Max = max };

        for (var i = 0; i < 100; i++)
        {
            var delay = range.GetDelay();
            NUnit.Framework.Assert.That(delay, Is.GreaterThanOrEqualTo(min));
            NUnit.Framework.Assert.That(delay, Is.LessThan(max));
        }
    }
}