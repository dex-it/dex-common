using Dex.Audit.Contracts.Interfaces;
using Dex.Audit.Contracts.Messages;
using Dex.Audit.Contracts.Options;
using Dex.Audit.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.Publisher.Services;

/// <summary>
/// Класс для управления событиями аудита
/// </summary>
internal sealed class AuditManager : IAuditManager
{
    private readonly IAuditPublisher _auditPublisher;
    private readonly AuditEventOptions _auditEventOptions;
    private readonly IAuditEventConfigurator _auditEventConfigurator;
    private readonly IRedisDatabase _redisDatabase;
    private readonly ILogger<AuditManager> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditManager"/>
    /// </summary>
    /// <param name="auditPublisher"><see cref="IAuditPublisher"/></param>
    /// <param name="auditEventConfigurator"><see cref="IAuditEventConfigurator"/></param>
    /// <param name="redisDatabase"><see cref="IRedisDatabase"/></param>
    /// <param name="auditEventOptions"><see cref="AuditEventOptions"/></param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
    public AuditManager(IAuditPublisher auditPublisher, IAuditEventConfigurator auditEventConfigurator, IOptions<AuditEventOptions> auditEventOptions,
        IRedisDatabase redisDatabase, ILogger<AuditManager> logger)
    {
        _auditPublisher = auditPublisher;
        _auditEventConfigurator = auditEventConfigurator;
        _redisDatabase = redisDatabase;
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
            AuditSettings? auditSettings = await _redisDatabase.GetAsync<AuditSettings>(eventBaseInfo.EventType.ToString());

            if (auditSettings is not null && auditSettings.SeverityLevel < _auditEventOptions.MinSeverityLevel)
            {
                return;
            }

            AuditEventMessage auditEvent = await _auditEventConfigurator.ConfigureAuditEventAsync(eventBaseInfo, cancellationToken);
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
