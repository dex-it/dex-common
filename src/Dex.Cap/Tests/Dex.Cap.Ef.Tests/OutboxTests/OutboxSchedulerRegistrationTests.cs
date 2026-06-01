using System;
using Dex.Cap.Outbox.AspNetScheduler.Options;
using Dex.Cap.Outbox.Ef.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

[TestFixture]
public class OutboxSchedulerRegistrationTests
{
    private static OutboxHandlerOptions Resolve(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<OutboxHandlerOptions>>().Value;
    }

    [Test]
    public void IntOverload_PreservesDefaultInitDelays()
    {
        var services = new ServiceCollection();
        services.AddDefaultOutboxScheduler<TestDbContext>(periodSeconds: 5, cleanupDays: 7);

        var options = Resolve(services);

        NUnit.Framework.Assert.Multiple((Action)Assertions);
        return;

        void Assertions()
        {
            NUnit.Framework.Assert.That(options.Period, Is.EqualTo(TimeSpan.FromSeconds(5)));
            NUnit.Framework.Assert.That(options.CleanupOlderThan, Is.EqualTo(TimeSpan.FromDays(7)));
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Min, Is.EqualTo(TimeSpan.FromSeconds(5)));
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Max, Is.EqualTo(TimeSpan.FromSeconds(15)));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Min, Is.EqualTo(TimeSpan.FromSeconds(20)));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Max, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }
    }

    [Test]
    public void ConfigureOverload_AppliesCustomInitDelays()
    {
        var services = new ServiceCollection();
        services.AddDefaultOutboxScheduler<TestDbContext>(o =>
        {
            o.HandlerInitDelay = new InitDelayRange { Min = TimeSpan.Zero, Max = TimeSpan.Zero };
            o.CleanerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(1), Max = TimeSpan.FromSeconds(1) };
        });

        var options = Resolve(services);

        NUnit.Framework.Assert.Multiple((Action)Assertions);
        return;

        void Assertions()
        {
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Min, Is.EqualTo(TimeSpan.Zero));
            NUnit.Framework.Assert.That(options.HandlerInitDelay.Max, Is.EqualTo(TimeSpan.Zero));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Min, Is.EqualTo(TimeSpan.FromSeconds(1)));
            NUnit.Framework.Assert.That(options.CleanerInitDelay.Max, Is.EqualTo(TimeSpan.FromSeconds(1)));
        }
    }

    [Test]
    public void ConfigureOverload_InvalidRange_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddDefaultOutboxScheduler<TestDbContext>(o =>
        {
            o.HandlerInitDelay = new InitDelayRange { Min = TimeSpan.FromSeconds(10), Max = TimeSpan.FromSeconds(1) };
        });

        NUnit.Framework.Assert.Throws<OptionsValidationException>((Action)(() =>
        {
            using var provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<IOptions<OutboxHandlerOptions>>().Value;
        }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void IntOverload_InvalidPeriod_ThrowsArgumentOutOfRange(int periodSeconds)
    {
        var services = new ServiceCollection();

        NUnit.Framework.Assert.Throws<ArgumentOutOfRangeException>((Action)Act);
        return;

        void Act() => services.AddDefaultOutboxScheduler<TestDbContext>(periodSeconds, cleanupDays: 1);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void IntOverload_InvalidCleanupDays_ThrowsArgumentOutOfRange(int cleanupDays)
    {
        var services = new ServiceCollection();

        NUnit.Framework.Assert.Throws<ArgumentOutOfRangeException>((Action)Act);
        return;

        void Act() => services.AddDefaultOutboxScheduler<TestDbContext>(periodSeconds: 1, cleanupDays: cleanupDays);
    }
}