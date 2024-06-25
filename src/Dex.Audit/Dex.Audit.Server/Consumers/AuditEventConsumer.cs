using AutoMapper;
using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Consumers;

/// <summary>
/// Обработчик аудиторских событий, полученных через шину сообщений
/// </summary>
public class AuditEventConsumer : IConsumer<AuditEventMessage>
{
    private readonly IMapper _mapper;
    private readonly IAuditRepository _auditRepository;
    private readonly IAuditSettingsRepository _auditSettingsRepository;
    private readonly ILogger<AuditEventConsumer> _logger;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AuditEventConsumer"/>
    /// </summary>
    /// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/></param>
    /// <param name="mapper"><see cref="IMapper"/></param>
    /// <param name="auditRepository"><see cref="IAuditRepository"/></param>
    /// <param name="logger"><see cref="ILogger"/></param>
    public AuditEventConsumer(
        IMapper mapper,
        IAuditRepository auditRepository,
        ILogger<AuditEventConsumer> logger,
        IAuditSettingsRepository auditSettingsRepository)
    {
        _mapper = mapper;
        _auditRepository = auditRepository;
        _logger = logger;
        _auditSettingsRepository = auditSettingsRepository;
    }

    /// <summary>
    /// Метод для обработки аудиторских событий, полученных через шину сообщений
    /// </summary>
    /// <param name="context">Контекст сообщения, содержащий аудиторское событие для обработки</param>
    public async Task Consume(ConsumeContext<AuditEventMessage> context)
    {
        string eventType = context.Message.EventType;
        string? sourceIp = context.Message.SourceIpAddress;

        _logger.LogInformation("Начало обработки сообщения аудита [{EventType}] от [{SourceIp}]", eventType, sourceIp);

        try
        {
            AuditEvent? auditEvent = _mapper.Map<AuditEvent>(context.Message);

            if (context.Message.AuditSettingsId is null)
            {
                AuditSettings? auditSettings = await _auditSettingsRepository.GetAsync(eventType);

                if (auditSettings is null)
                {
                    _logger.LogWarning("Не удалось получить из кэша настройки для события типа [{EventType}]", eventType);
                    throw new Exception(nameof(auditSettings));
                }

                if (auditSettings.SeverityLevel < context.Message.SourceMinSeverityLevel)
                {
                    return;
                }
            }

            SetDestinationDate(auditEvent);

            await _auditRepository.AddAuditEventAsync(auditEvent, context.CancellationToken);

            _logger.LogInformation("Сообщение аудита [{EventType}] от [{SourceIp}] успешно обработано", eventType, sourceIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникла ошибка при обработке сообщения аудита [{EventType}] от [{SourceIp}]", eventType, sourceIp);
            throw;
        }
    }

    private static void SetDestinationDate(AuditEvent auditEvent)
    {
        if (auditEvent.Destination.GmtDate == DateTime.MinValue)
        {
            auditEvent.Destination.GmtDate = DateTime.UtcNow;
        }

        if (auditEvent.Destination.End == DateTime.MinValue)
        {
            auditEvent.Destination.End = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        }
    }
}
