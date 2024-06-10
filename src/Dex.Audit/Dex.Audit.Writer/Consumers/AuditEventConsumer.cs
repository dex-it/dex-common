using AutoMapper;
using Dex.Audit.Contracts.Messages;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Domain.Models;
using Dex.Audit.Domain.Models.AuditEvent;
using Dex.Audit.Persistence;
using MassTransit;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Dex.Audit.Writer.Consumers;

/// <summary>
/// Обработчик аудиторских событий, полученных через шину сообщений
/// </summary>
public class AuditEventConsumer : IConsumer<AuditEventMessage>
{
    private readonly IMapper _mapper;
    private readonly IAuditContext _context;
    private readonly IRedisDatabase _redisDatabase;
    private readonly ILogger<AuditEventConsumer> _logger;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AuditEventConsumer"/>
    /// </summary>
    /// <param name="redisDatabase"><see cref="IRedisDatabase"/></param>
    /// <param name="mapper"><see cref="IMapper"/></param>
    /// <param name="context"><see cref="AuditContext"/></param>
    /// <param name="logger"><see cref="ILogger"/></param>
    public AuditEventConsumer(IMapper mapper, IAuditContext context, ILogger<AuditEventConsumer> logger, IRedisDatabase redisDatabase)
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;
        _redisDatabase = redisDatabase;
    }

    /// <summary>
    /// Метод для обработки аудиторских событий, полученных через шину сообщений
    /// </summary>
    /// <param name="context">Контекст сообщения, содержащий аудиторское событие для обработки</param>
    public async Task Consume(ConsumeContext<AuditEventMessage> context)
    {
        AuditEventType eventType = context.Message.EventType;
        string? sourceIp = context.Message.SourceIpAddress;

        _logger.LogInformation("Начало обработки сообщения аудита [{EventType}] от [{SourceIp}]", eventType, sourceIp);

        try
        {
            AuditEvent? auditEvent = _mapper.Map<AuditEvent>(context.Message);

            if (context.Message.AuditSettingsId is null)
            {
                AuditSettings? auditSettings = await _redisDatabase.GetAsync<AuditSettings>(eventType.ToString());

                if (auditSettings is null)
                {
                    _logger.LogWarning("Не удалось получить из кэша настройки для события типа [{EventType}]", eventType);
                    throw new Exception(nameof(auditSettings));
                }

                if (auditSettings.SeverityLevel < context.Message.SourceMinSeverityLevel)
                {
                    return;
                }

                auditEvent.AuditSettingsId = auditSettings.Id;
            }

            SetDestinationDate(auditEvent);

            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync(context.CancellationToken);

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
