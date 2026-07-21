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
    public async Task StartHost_InvalidInboxOptions_FailsAtStartup()
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

                // Иначе под тест-раннером Rider warm-up реестра упал бы дублем ALC раньше, чем ValidateOnStart
                // добрался до опций, и тест ловил бы не то исключение.
                UseSingleAssemblyInboxTypeSource(services);
            })
            .Build();

        // Прямой await, а не ThrowsAsync с лямбдой: замыкание над using-переменной host дало бы ложный AccessToDisposedClosure.
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

        // Вместе с именем типа опций: аутбокс объявляет одноимённую опцию, а склейка отказов в
        // OptionsValidationException.Message тип не несёт, он остаётся только в OptionsType.
        Assert.IsTrue(
            ex!.Message.Contains($"{nameof(InboxOptions)}.{nameof(InboxOptions.ConcurrencyLimit)}", StringComparison.Ordinal),
            ex.Message);
    }
}