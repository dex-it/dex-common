using System.Reflection;
using Dex.Audit.Contracts.Interfaces;
using Dex.Audit.Contracts.Messages;
using Dex.Audit.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dex.Audit.Publisher.Workers;

/// <summary>
/// Сервис, отвечающий за выполнение аудита подсистемы
/// </summary>
public class SubsystemAuditWorker : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Конструктор класса SubsystemAuditWorker
    /// </summary>
    /// <param name="scopeFactory"><see cref="IServiceScopeFactory"/></param>
    public SubsystemAuditWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Запускает выполнение аудита при запуске подсистемы
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync(AuditEventType.StartSubsystem, "Начало работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Завершает выполнение аудита при завершении работы подсистемы
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync(AuditEventType.ShutdownSubsystem, "Окончание работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Выполняет аудит события подсистемы
    /// </summary>
    /// <param name="eventType">Тип события аудита</param>
    /// <param name="description">Описание события</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task AuditSubsystemEventAsync(AuditEventType eventType, string description, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAuditManager auditManager = scope.ServiceProvider.GetRequiredService<IAuditManager>();

        await auditManager.ProcessAuditEventAsync(
            new AuditEventBaseInfo(eventType, GetSourceAssemblyName(), description, true),
            cancellationToken);
    }

    private static string? GetSourceAssemblyName()
    {
        return Assembly.GetEntryAssembly()?.FullName;
    }
}
