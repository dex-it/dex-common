using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Dex.Cap.Inbox.AspNetScheduler.BackgroundServices;

/// <summary>
/// Строит реестр типов сообщений на старте хоста.
/// </summary>
/// <remarks>
/// Реестр строится по сборкам рефлексией и может обнаружить ошибку конфигурации: один дискриминатор у
/// двух типов, пустой дискриминатор, кавычка в дискриминаторе. Без этого сервиса реестр строился бы
/// лениво, уже внутри фонового обработчика, где цикл перехватывает исключение и пишет LogCritical:
/// хост считался бы поднятым, а инбокс молча не обрабатывал бы ничего.
/// <para>
/// Это <see cref="IHostedService"/>, а не <see cref="BackgroundService"/>: работа выполняется в
/// <see cref="StartAsync"/> синхронно, поэтому исключение роняет старт хоста, как и задумано.
/// </para>
/// </remarks>
internal sealed class InboxRegistryWarmupService(IInboxTypeDiscriminatorProvider discriminatorProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        discriminatorProvider.Warmup();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
