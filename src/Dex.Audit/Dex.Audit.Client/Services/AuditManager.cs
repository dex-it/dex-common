using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Options;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Services;

/// <summary>
/// Класс для управления событиями аудита
/// </summary>
internal sealed class AuditManager : IAuditManager
{
    private readonly IAuditPublisher _auditPublisher;
    private readonly AuditEventOptions _auditEventOptions;
    private readonly IAuditEventConfigurator _auditEventConfigurator;
    private readonly IAuditSettingsRepository _auditSettingsRepository;
    private readonly ILogger<AuditManager> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditManager"/>
    /// </summary>
    /// <param name="auditPublisher"><see cref="IAuditPublisher"/></param>
    /// <param name="auditEventConfigurator"><see cref="IAuditEventConfigurator"/></param>
    /// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/></param>
    /// <param name="auditEventOptions"><see cref="AuditEventOptions"/></param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
    public AuditManager(
        IAuditPublisher auditPublisher,
        IAuditEventConfigurator auditEventConfigurator,
        IOptions<AuditEventOptions> auditEventOptions,
        IAuditSettingsRepository auditSettingsRepository,
        ILogger<AuditManager> logger)
    {
        _auditPublisher = auditPublisher;
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
    public async Task ProcessAuditEventAsync(AuditEventBaseInfo eventBaseInfo, CancellationToken cancellationToken = default)
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

            await _auditPublisher.PublishEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникла ошибка при обработке сообщения аудита");
            throw;
        }
    }
}
