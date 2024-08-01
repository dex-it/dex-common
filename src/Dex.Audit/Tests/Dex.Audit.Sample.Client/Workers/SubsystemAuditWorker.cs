using System.Reflection;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;

namespace Dex.Audit.ClientSample.Workers;

/// <summary>
/// Сервис, отвечающий за выполнение аудита подсистемы.
/// </summary>
public class SubsystemAuditWorker : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Конструктор класса SubsystemAuditWorker.
    /// </summary>
    /// <param name="scopeFactory"><see cref="IServiceScopeFactory"/></param>
    public SubsystemAuditWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Запускает выполнение аудита при запуске подсистемы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync("StartSubsystem", "Начало работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Завершает выполнение аудита при завершении работы подсистемы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync("ShutdownSubsystem", "Окончание работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Выполняет аудит события подсистемы.
    /// </summary>
    /// <param name="eventType">Тип события аудита</param>
    /// <param name="description">Описание события</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task AuditSubsystemEventAsync(string eventType, string description, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var auditManager = scope.ServiceProvider.GetRequiredService<IAuditWriter>();

        await auditManager.WriteAsync(
            new AuditEventBaseInfo(eventType, GetSourceAssemblyName(), description, true),
            cancellationToken);
    }

    private static string? GetSourceAssemblyName()
    {
        return Assembly.GetEntryAssembly()?.FullName;
    }
}
