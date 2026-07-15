using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Handlers;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Ef.Extensions;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Health check инбокса и его регистрация.
/// </summary>
public class InboxHealthCheckTests : BaseTest
{
    [Test]
    public void Scheduler_RegistersHealthCheck_RegardlessOfAddHealthChecksOrder()
    {
        // Порядок вызова AddHealthChecks не важен: планировщик регистрирует его сам, а сам
        // AddHealthChecks идемпотентен. У Outbox порядок важен, поэтому проверяем оба.
        Assert.AreEqual(1, CountInboxRegistrations(services => services
            .AddScoped(_ => new TestDbContext(DbName))
            .AddInbox<TestDbContext>()
            .AddDefaultInboxScheduler<TestDbContext>()));

        Assert.AreEqual(1, CountInboxRegistrations(services => services
            .AddScoped(_ => new TestDbContext(DbName))
            .AddInbox<TestDbContext>()
            .AddDefaultInboxScheduler<TestDbContext>()
            .AddHealthChecks().Services));

        Assert.AreEqual(1, CountInboxRegistrations(services =>
        {
            services.AddHealthChecks();
            return services
                .AddScoped(_ => new TestDbContext(DbName))
                .AddInbox<TestDbContext>()
                .AddDefaultInboxScheduler<TestDbContext>();
        }));
    }

    [Test]
    public void Scheduler_RegisteredTwice_DoesNotDuplicateHealthCheck()
    {
        // Маркер в AddInboxScheduler существует ровно для этого.
        Assert.AreEqual(1, CountInboxRegistrations(services => services
            .AddScoped(_ => new TestDbContext(DbName))
            .AddInbox<TestDbContext>()
            .AddDefaultInboxScheduler<TestDbContext>()
            .AddDefaultInboxScheduler<TestDbContext>()));
    }

    [Test]
    public async Task HealthCheck_ReportsDegraded_WhenNoCycleHappenedForTwoPeriods()
    {
        var sp = BuildProvider(services => services
            .AddScoped(_ => new TestDbContext(DbName))
            .AddInbox<TestDbContext>()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, TestInboxCommandHandler>()
            .AddDefaultInboxScheduler<TestDbContext>(periodSeconds: 1, cleanupDays: 30));

        var check = sp.GetRequiredService<HealthCheckService>();

        // Свежий коллектор ставит метку при создании, поэтому сразу после старта состояние здоровое.
        var healthy = await check.CheckHealthAsync(r => r.Tags.Contains("inbox-scheduler"), CancellationToken.None);
        Assert.AreEqual(HealthStatus.Healthy, healthy.Status);

        // Period = 1s, порог 2 x Period. Ждём дольше, ни одного цикла за это время не было.
        await Task.Delay(TimeSpan.FromSeconds(3));

        var degraded = await check.CheckHealthAsync(r => r.Tags.Contains("inbox-scheduler"), CancellationToken.None);
        Assert.AreEqual(HealthStatus.Degraded, degraded.Status);
    }

    private static int CountInboxRegistrations(Func<IServiceCollection, IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        AddLogging(services);
        configure(services);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations.Count(r => r.Name == "inbox-scheduler");
    }

    private ServiceProvider BuildProvider(Func<IServiceCollection, IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        AddLogging(services);
        configure(services);
        return services.BuildServiceProvider();
    }
}
