using Dex.Audit.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dex.Audit.Logger;

internal class AuditLoggerReader(IServiceProvider serviceScopeFactory) : BackgroundService 
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var auditManager = scope.ServiceProvider.GetRequiredService<IAuditManager>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await AuditLogger.BaseInfoChannel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false);

            if (!AuditLogger.BaseInfoChannel.Reader.TryRead(out var auditEventBaseInfo))
            {
                continue;
            }

            await auditManager.ProcessAuditEventAsync(auditEventBaseInfo, stoppingToken).ConfigureAwait(false);
        }
    }
}