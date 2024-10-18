using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// A class for managing audit events.
/// </summary>
/// <param name="auditOutputProvider"><see cref="IAuditOutputProvider"/></param>
/// <param name="auditEventConfigurator"><see cref="IAuditEventConfigurator"/></param>
/// <param name="auditSettingsService"><see cref="IAuditSettingsCacheRepository"/></param>
/// <param name="auditEventOptions"><see cref="AuditEventOptions"/></param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
internal sealed class AuditWriter(
    IAuditOutputProvider auditOutputProvider,
    IAuditEventConfigurator auditEventConfigurator,
    IOptions<AuditEventOptions> auditEventOptions,
    IAuditSettingsService auditSettingsService,
    ILogger<AuditWriter> logger)
    : IAuditWriter
{
    /// <summary>
    /// Processes and publishes an audit event.
    /// </summary>
    /// <param name="eventBaseInfo">Basic information about the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task WriteAsync(
        AuditEventBaseInfo eventBaseInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditSettings = await auditSettingsService
                .GetOrGetAndUpdateSettingsAsync(eventBaseInfo.EventType, cancellationToken)
                .ConfigureAwait(false);

            if (auditSettings is not null && auditSettings.SeverityLevel < auditEventOptions.Value.MinSeverityLevel)
            {
                return;
            }

            var auditEvent = await auditEventConfigurator
                .ConfigureAuditEventAsync(eventBaseInfo, cancellationToken)
                .ConfigureAwait(false);

            auditEvent.SourceMinSeverityLevel = auditEventOptions.Value.MinSeverityLevel;
            auditEvent.AuditSettingsId = auditSettings?.Id;

            await auditOutputProvider
                .PublishEventAsync(auditEvent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing audit messages.");
            throw;
        }
    }
}
