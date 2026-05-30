using System;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests;

[TestFixture]
public class InitDelayRangeTests
{
    [Test]
    public void GetDelay_WhenMinEqualsMax_ReturnsThatValue()
    {
        var range = new InitDelayRange { Min = TimeSpan.FromSeconds(7), Max = TimeSpan.FromSeconds(7) };

        Assert.That(range.GetDelay(), Is.EqualTo(TimeSpan.FromSeconds(7)));
    }

    [Test]
    public void GetDelay_WhenZeroRange_ReturnsZero()
    {
        var range = new InitDelayRange { Min = TimeSpan.Zero, Max = TimeSpan.Zero };

        Assert.That(range.GetDelay(), Is.EqualTo(TimeSpan.Zero));
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
            Assert.That(delay, Is.GreaterThanOrEqualTo(min));
            Assert.That(delay, Is.LessThan(max));
        }
    }
}
