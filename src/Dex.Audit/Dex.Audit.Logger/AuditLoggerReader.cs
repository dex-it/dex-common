using Dex.Audit.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

/// <summary>
/// Фоновая служба для чтения и отправки событий в очередь из <see cref="AuditLogger.BaseInfoChannel"/>.
/// </summary>
/// <param name="serviceScopeFactory"><see cref="IServiceProvider"/></param>
internal sealed class AuditLoggerReader(IServiceProvider serviceScopeFactory, ILogger<AuditLoggerReader> logger) : BackgroundService 
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var auditManager = scope.ServiceProvider.GetRequiredService<IAuditManager>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AuditLogger.BaseInfoChannel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false);

                if (!AuditLogger.BaseInfoChannel.Reader.TryRead(out var auditEventBaseInfo))
                {
                    continue;
                }

                await auditManager.ProcessAuditEventAsync(auditEventBaseInfo, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occured while trying to read auditable events: {Message}", exception.Message);
            }
            
        }
    }
}