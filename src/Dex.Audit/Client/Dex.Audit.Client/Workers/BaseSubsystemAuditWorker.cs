using System.Reflection;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Workers;

/// <summary>
/// Service responsible for performing audit of the subsystem.
/// </summary>
public class BaseSubsystemAuditWorker(IServiceScopeFactory scopeFactory, ILogger<BaseSubsystemAuditWorker> logger) : IHostedService
{
    /// <summary>
    /// Starts the audit when the subsystem starts.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync("StartSystem", "Subsystem startup", cancellationToken);
    }

    /// <summary>
    /// Completes the audit when the subsystem shuts down.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return AuditSubsystemEventAsync("ShutdownSystem", "Subsystem shutdown", cancellationToken);
    }

    /// <summary>
    /// Performs the audit of a subsystem event.
    /// </summary>
    /// <param name="eventType">The type of audit event</param>
    /// <param name="description">Event description</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task AuditSubsystemEventAsync(string eventType, string description, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var auditManager = scope.ServiceProvider.GetRequiredService<IAuditWriter>();

            await auditManager.WriteAsync(
                new AuditEventBaseInfo(eventType, GetSourceAssemblyName(), description, true),
                cancellationToken)
                .ConfigureAwait(false);
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
