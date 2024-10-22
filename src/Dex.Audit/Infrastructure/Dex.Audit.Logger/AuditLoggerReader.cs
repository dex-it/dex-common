using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Logger.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Logger;

/// <summary>
/// A background service for reading and sending events to a queue from <see cref="AuditLogger.BaseInfoChannel"/>.
/// </summary>
/// <param name="serviceScopeFactory"><see cref="IServiceProvider"/></param>
internal sealed class AuditLoggerReader(
    IServiceProvider serviceScopeFactory,
    ILogger<AuditLoggerReader> logger,
    IOptions<AuditLoggerOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var auditWriter = scope.ServiceProvider.GetRequiredService<IAuditWriter>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await AuditLogger.BaseInfoChannel.Reader
                    .WaitToReadAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!AuditLogger.BaseInfoChannel.Reader
                        .TryRead(out var auditEventBaseInfo))
                {
                    continue;
                }

                await auditWriter
                    .WriteAsync(auditEventBaseInfo, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "An error occured while trying to read auditable events: {Message}",
                    exception.Message);
            }

            await Task
                .Delay(options.Value.ReadEventsInterval, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}