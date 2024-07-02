using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Класс для управления событиями аудита
/// </summary>
internal sealed class AuditWriter : IAuditWriter
{
    private readonly IAuditOutputProvider _auditOutputProvider;
    private readonly AuditEventOptions _auditEventOptions;
    private readonly IAuditEventConfigurator _auditEventConfigurator;
    private readonly IAuditSettingsRepository _auditSettingsRepository;
    private readonly ILogger<AuditWriter> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditWriter"/>
    /// </summary>
    /// <param name="auditOutputProvider"><see cref="IAuditOutputProvider"/></param>
    /// <param name="auditEventConfigurator"><see cref="IAuditEventConfigurator"/></param>
    /// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/></param>
    /// <param name="auditEventOptions"><see cref="AuditEventOptions"/></param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
    public AuditWriter(
        IAuditOutputProvider auditOutputProvider,
        IAuditEventConfigurator auditEventConfigurator,
        IOptions<AuditEventOptions> auditEventOptions,
        IAuditSettingsRepository auditSettingsRepository,
        ILogger<AuditWriter> logger)
    {
        _auditOutputProvider = auditOutputProvider;
        _auditEventConfigurator = auditEventConfigurator;
        _auditSettingsRepository = auditSettingsRepository;
        _logger = logger;
        _auditEventOptions = auditEventOptions.Value;
    }

    /// <summary>
    /// Обрабатывает и публикует событие аудита
    /// </summary>
    /// <param name="eventBaseInfo">Базовая информация о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task WriteAsync(AuditEventBaseInfo eventBaseInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditSettings = await _auditSettingsRepository
                .GetAsync(eventBaseInfo.EventType, cancellationToken)
                .ConfigureAwait(false);

            if (auditSettings is not null && auditSettings.SeverityLevel < _auditEventOptions.MinSeverityLevel)
            {
                return;
            }

            var auditEvent = await _auditEventConfigurator.ConfigureAuditEventAsync(eventBaseInfo, cancellationToken);
            auditEvent.SourceMinSeverityLevel = _auditEventOptions.MinSeverityLevel;
            auditEvent.AuditSettingsId = auditSettings?.Id;

            await _auditOutputProvider.PublishEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникла ошибка при обработке сообщения аудита");
            throw;
        }
    }
}
