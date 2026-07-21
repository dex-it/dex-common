using System;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

/// <summary>
/// Правило про размер тела применяется на старте хоста. ValidateOnStart срабатывает только там, а не при
/// BuildServiceProvider, поэтому без такой проверки правило существовало бы на бумаге. Сами правила
/// проверяет <see cref="OutboxOptionsValidationTests"/>.
/// </summary>
/// <remarks>
/// Фикстура не наследует <see cref="BaseTest"/>: старт хоста доходит только до валидации опций, до
/// <see cref="TestDbContext"/> дело не доходит вовсе, поэтому база здесь не нужна.
/// </remarks>
[TestFixture]
public class OutboxOptionsStartupTests
{
    [Test]
    public async Task StartHost_NonPositiveMaxContentLength_FailsAtStartup()
    {
        // Прямой await, а не ThrowsAsync с лямбдой: замыкание над using-переменной host дало бы ложный
        // AccessToDisposedClosure.
        using var host = BuildHost(options => options.MaxContentLength = 0);

        OptionsValidationException? ex = null;
        try
        {
            await host.StartAsync();
        }
        catch (OptionsValidationException e)
        {
            ex = e;
        }

        Assert.IsNotNull(ex);
        Assert.IsTrue(ex!.Message.Contains(nameof(OutboxOptions.MaxContentLength), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Остальные правила <see cref="OutboxOptionsValidator"/> обязаны остаться спящими: их включение уронило
    /// бы на старте сервисы, чьи конфиги годами принимались молча.
    /// </summary>
    /// <remarks>
    /// Значения не произвольные: <c>GetFreeMessagesTimeout</c> в 10 мс это ровно конфиг живого потребителя
    /// (<c>Dex.Events.Distributed.Tests/BaseTest.cs</c>), то есть доказательство, что подключать валидатор
    /// целиком нельзя. Остальные подобраны так, чтобы задеть все пять спящих правил: ноль попыток, партия
    /// меньше параллелизма и сам параллелизм.
    /// </remarks>
    [Test]
    public async Task StartHost_ValuesRejectedByTheDormantRules_StillStarts()
    {
        using var host = BuildHost(options =>
        {
            options.GetFreeMessagesTimeout = TimeSpan.FromMilliseconds(10);
            options.Retries = 0;
            options.MessagesToProcess = 5;
            options.ConcurrencyLimit = 50;
        });

        await host.StartAsync();
        await host.StopAsync();

        var options = host.Services.GetRequiredService<IOptions<OutboxOptions>>().Value;
        Assert.AreEqual(TimeSpan.FromMilliseconds(10), options.GetFreeMessagesTimeout);
        Assert.AreEqual(0, options.Retries);
        Assert.AreEqual(50, options.ConcurrencyLimit);
    }

    /// <remarks>
    /// <see cref="TestDbContext"/> не регистрируется намеренно: старт доходит только до валидации опций,
    /// <c>AddOutbox</c> фоновых служб не поднимает, и контекст никто не резолвит. Регистрация с фиксированным
    /// именем БД была бы мёртвой, но при первом же появлении hosted service повела бы тест в чужую базу.
    /// </remarks>
    private static IHost BuildHost(Action<OutboxOptions> configure)
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOutbox<TestDbContext>();

                services.Configure(configure);
            })
            .Build();
    }
}