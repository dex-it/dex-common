using System.Reflection;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Sample.Shared.Enums;

namespace Dex.Audit.ClientSample.Infrastructure.Workers;

/// <summary>
/// Сервис, отвечающий за выполнение аудита подсистемы.
/// </summary>
public sealed class SubsystemAuditWorker(IServiceScopeFactory scopeFactory, ILogger<SubsystemAuditWorker> logger) : IHostedService
{

    /// <summary>
    /// Запускает выполнение аудита при запуске подсистемы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync(AuditEventType.StartSystem.ToString(), "Начало работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Завершает выполнение аудита при завершении работы подсистемы.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync(AuditEventType.ShutdownSystem.ToString(), "Окончание работы подсистемы", cancellationToken);
    }

    /// <summary>
    /// Выполняет аудит события подсистемы.
    /// </summary>
    /// <param name="eventType">Тип события аудита</param>
    /// <param name="description">Описание события</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task AuditSubsystemEventAsync(string eventType, string description, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var auditManager = scope.ServiceProvider.GetRequiredService<IAuditWriter>();

            await auditManager.WriteAsync(
                new AuditEventBaseInfo(eventType, GetSourceAssemblyName(), description, true),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
        }
    }

    private static string? GetSourceAssemblyName()
    {
        return Assembly.GetEntryAssembly()?.FullName;
    }
}
