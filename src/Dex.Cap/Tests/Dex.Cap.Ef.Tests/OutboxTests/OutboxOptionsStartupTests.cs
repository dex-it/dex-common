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
    public async Task StartHost_NonPositiveMaxContentLengthBytes_FailsAtStartup()
    {
        // Прямой await, а не ThrowsAsync с лямбдой: замыкание над using-переменной host дало бы ложный
        // AccessToDisposedClosure.
        using var host = BuildHost(options => options.MaxContentLengthBytes = 0);

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

        // Ждём имя опции ВМЕСТЕ с типом: OptionsValidationException.Message это склейка отказов, тип
        // остаётся только в OptionsType, а одноимённую опцию объявляет и инбокс. Проверка по голому
        // MaxContentLengthBytes пропустила бы потерю префикса.
        Assert.IsTrue(
            ex!.Message.Contains($"{nameof(OutboxOptions)}.{nameof(OutboxOptions.MaxContentLengthBytes)}", StringComparison.Ordinal),
            ex.Message);
    }

    /// <summary>
    /// Остальные правила <see cref="OutboxOptionsValidator"/> обязаны остаться спящими: их включение уронило
    /// бы на старте сервисы, чьи конфиги годами принимались молча.
    /// </summary>
    /// <remarks>
    /// Значения подобраны так, чтобы задеть КАЖДОЕ из пяти спящих правил, иначе тест доказывал бы
    /// толерантность только к части из них: <c>Retries=0</c> и <c>MessagesToProcess=0</c> отвергаются
    /// правилами на положительность, <c>ConcurrencyLimit=101</c> выходит за верхнюю границу 100, он же
    /// превышает <c>MessagesToProcess</c>, а <c>GetFreeMessagesTimeout</c> ниже секунды. Границы важны:
    /// 5 и 50 правила на диапазон 1..100 НЕ нарушают.
    /// <para>
    /// Таймаут в 10 мс не произволен, это ровно конфиг живого потребителя
    /// (<c>Dex.Events.Distributed.Tests/BaseTest.cs</c>, дефолт параметра <c>getFreeMessagesTimeout</c>),
    /// то есть доказательство, что подключать валидатор целиком нельзя. Второй такой конфиг лежит в этой же
    /// сборке: <c>ExecuteTransactionOutboxTests.ParallelProcessingMessagesTest</c> берёт
    /// <c>MessagesToProcess=1</c> при дефолтном <c>ConcurrencyLimit=2</c>.
    /// </para>
    /// </remarks>
    [Test]
    public async Task StartHost_ValuesRejectedByTheDormantRules_StillStarts()
    {
        using var host = BuildHost(options =>
        {
            options.GetFreeMessagesTimeout = TimeSpan.FromMilliseconds(10);
            options.Retries = 0;
            options.MessagesToProcess = 0;
            options.ConcurrencyLimit = 101;
        });

        await host.StartAsync();
        await host.StopAsync();

        var options = host.Services.GetRequiredService<IOptions<OutboxOptions>>().Value;
        Assert.AreEqual(TimeSpan.FromMilliseconds(10), options.GetFreeMessagesTimeout);
        Assert.AreEqual(0, options.Retries);
        Assert.AreEqual(0, options.MessagesToProcess);
        Assert.AreEqual(101, options.ConcurrencyLimit);
    }

    /// <summary>
    /// Обратная сторона предыдущего теста и цена, которую README числит в Breaking changes:
    /// <c>ValidateOnStart</c> взводится на экземпляр опций, а не на конкретный валидатор, поэтому валидатор,
    /// зарегистрированный САМИМ потребителем, тоже исполнится на старте.
    /// </summary>
    /// <remarks>
    /// Единственный способ получить эти правила до 8.5 состоял в том, чтобы зарегистрировать публичный
    /// <see cref="OutboxOptionsValidator"/> вручную. Раньше такая регистрация всплывала позже, на первой
    /// материализации опций; теперь роняет запуск. Без этого теста утверждение жило бы только в тексте
    /// README, а обойти механизм нельзя: точечной альтернативы в Options API нет.
    /// </remarks>
    [Test]
    public async Task StartHost_ConsumerRegisteredFullValidator_FailsAtStartup()
    {
        using var host = BuildHost(
            options => options.GetFreeMessagesTimeout = TimeSpan.FromMilliseconds(10),
            services => services.AddSingleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidator>());

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
        Assert.IsTrue(ex!.Message.Contains(nameof(OutboxOptions.GetFreeMessagesTimeout), StringComparison.Ordinal), ex.Message);
    }

    /// <remarks>
    /// <see cref="TestDbContext"/> не регистрируется намеренно: старт доходит только до валидации опций,
    /// <c>AddOutbox</c> фоновых служб не поднимает, и контекст никто не резолвит. Регистрация с фиксированным
    /// именем БД была бы мёртвой, но при первом же появлении hosted service повела бы тест в чужую базу.
    /// </remarks>
    private static IHost BuildHost(Action<OutboxOptions> configure, Action<IServiceCollection>? configureServices = null)
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOutbox<TestDbContext>();

                services.Configure(configure);

                configureServices?.Invoke(services);
            })
            .Build();
    }
}