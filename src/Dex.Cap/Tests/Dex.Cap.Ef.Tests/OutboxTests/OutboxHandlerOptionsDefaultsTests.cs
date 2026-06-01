using System;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

[TestFixture]
public class OutboxHandlerOptionsDefaultsTests
{
    [Test]
    public void Defaults_PreserveHardcodedInitDelays()
    {
        var options = new OutboxHandlerOptions();

        NUnit.Framework.Assert.Multiple((Action)Assertions);
        return;

        void Assertions()
        {
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Min, Is.EqualTo(TimeSpan.FromSeconds(5)));
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Max, Is.EqualTo(TimeSpan.FromSeconds(15)));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Min, Is.EqualTo(TimeSpan.FromSeconds(20)));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Max, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }
    }
}