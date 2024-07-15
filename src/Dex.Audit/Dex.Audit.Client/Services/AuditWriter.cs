using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Класс для управления событиями аудита
/// </summary>
/// <param name="auditOutputProvider"><see cref="IAuditOutputProvider"/></param>
/// <param name="auditEventConfigurator"><see cref="IAuditEventConfigurator"/></param>
/// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/></param>
/// <param name="auditEventOptions"><see cref="AuditEventOptions"/></param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
internal sealed class AuditWriter(
    IAuditOutputProvider auditOutputProvider,
    IAuditEventConfigurator auditEventConfigurator,
    IOptions<AuditEventOptions> auditEventOptions,
    IAuditSettingsRepository auditSettingsRepository,
    ILogger<AuditWriter> logger) : IAuditWriter
{
    /// <summary>
    /// Обрабатывает и публикует событие аудита
    /// </summary>
    /// <param name="eventBaseInfo">Базовая информация о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task WriteAsync(AuditEventBaseInfo eventBaseInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditSettings = await auditSettingsRepository
                .GetAsync(eventBaseInfo.EventType, cancellationToken)
                .ConfigureAwait(false);

            if (auditSettings is not null && auditSettings.SeverityLevel < auditEventOptions.Value.MinSeverityLevel)
            {
                return;
            }

            var auditEvent = await auditEventConfigurator.ConfigureAuditEventAsync(eventBaseInfo, cancellationToken).ConfigureAwait(false);
            auditEvent.SourceMinSeverityLevel = auditEventOptions.Value.MinSeverityLevel;
            auditEvent.AuditSettingsId = auditSettings?.Id;

            await auditOutputProvider.PublishEventAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Возникла ошибка при обработке сообщения аудита");
            throw;
        }
    }
}
