using System;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Правила валидатора опций применяются на старте хоста. ValidateOnStart срабатывает только там,
/// а не при BuildServiceProvider, поэтому без такой проверки правила существовали бы на бумаге.
/// Сами правила проверяет <see cref="InboxOptionsValidationTests"/>.
/// </summary>
public class InboxOptionsStartupTests : BaseTest
{
    [Test]
    public void StartHost_InvalidInboxOptions_FailsAtStartup()
    {
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
}