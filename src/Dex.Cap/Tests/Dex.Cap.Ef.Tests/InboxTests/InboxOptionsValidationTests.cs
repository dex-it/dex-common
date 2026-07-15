using System;
using System.Threading.Tasks;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Валидаторы опций. Проверяются через реальный старт хоста: ValidateOnStart срабатывает только там,
/// а не при BuildServiceProvider, поэтому иначе правила существовали бы только на бумаге.
/// </summary>
public class InboxOptionsValidationTests : BaseTest
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
    public void StartHost_InvalidInboxOptions_FailsAtStartup()
    {
        // Главное в этом тесте: правило вообще применяется на старте, а не лежит мёртвым.
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                AddLogging(services);
                services
                    .AddScoped(_ => new TestDbContext(DbName))
                    .AddInbox<TestDbContext>(options =>
                    {
                        options.MessagesToProcess = 5;
                        options.ConcurrencyLimit = 50;
                    });
            })
            .Build();

        var ex = NUnit.Framework.Assert.ThrowsAsync<OptionsValidationException>(
            (Func<Task>)(async () => await host.StartAsync()));

        Assert.IsTrue(ex!.Message.Contains(nameof(InboxOptions.ConcurrencyLimit), StringComparison.Ordinal));
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
