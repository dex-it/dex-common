using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.ValueObjects;
using Dex.Audit.Server.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Consumers;

/// <summary>
/// Обработчик аудиторских событий, полученных через шину сообщений.
/// </summary>
public class AuditEventConsumer : IConsumer<AuditEventMessage>
{
    private readonly IAuditRepository _auditRepository;
    private readonly IAuditSettingsRepository _auditSettingsRepository;
    private readonly ILogger<AuditEventConsumer> _logger;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AuditEventConsumer"/>.
    /// </summary>
    /// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/>.</param>
    /// <param name="auditRepository"><see cref="IAuditRepository"/>.</param>
    /// <param name="logger"><see cref="ILogger"/>.</param>
    public AuditEventConsumer(
        IAuditRepository auditRepository,
        ILogger<AuditEventConsumer> logger,
        IAuditSettingsRepository auditSettingsRepository)
    {
        _auditRepository = auditRepository;
        _logger = logger;
        _auditSettingsRepository = auditSettingsRepository;
    }

    /// <summary>
    /// Метод для обработки аудиторских событий, полученных через шину сообщений.
    /// </summary>
    /// <param name="context">Контекст сообщения, содержащий аудиторское событие для обработки.</param>
    public async Task Consume(ConsumeContext<AuditEventMessage> context)
    {
        var eventType = context.Message.EventType;
        var sourceIp = context.Message.SourceIpAddress;

        _logger.LogInformation("Начало обработки сообщения аудита [{EventType}] от [{SourceIp}]", eventType, sourceIp);

        try
        {
            var auditEvent = MapAuditEventFromMessage(context.Message);

            if (context.Message.AuditSettingsId is null)
            {
                var auditSettings = await _auditSettingsRepository.GetAsync(eventType);

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

    private static AuditEvent MapAuditEventFromMessage(AuditEventMessage message)
    {
        return new AuditEvent
        {
            ExternalId = message.ExternalId,
            EventCode = message.EventCode,
            Source = new Source
            {
                Device = new Device
                {
                    Vendor = message.DeviceVendor,
                    Version = message.DeviceVersion,
                    Product = message.DeviceProduct,
                    ProcessName = message.DeviceProcessName,
                    EventClassId = message.DeviceEventClassId
                },
                UserDetails = new UserDetails
                {
                    User = message.SourceUser,
                    UserDomain = message.SourceUserDomain
                },
                AddressInfo = new AddressInfo
                {
                    IpAddress = message.SourceIpAddress,
                    MacAddress = message.SourceMacAddress,
                    DnsName = message.SourceDnsName,
                    Host = message.SourceHost
                },
                Port = message.SourcePort,
                Protocol = message.SourceProtocol,
                Start = message.Start,
                GmtDate = message.SourceGmtDate
            },
            Destination = new Destination
            {
                UserDetails = new UserDetails
                {
                    User = message.DestinationUser,
                    UserDomain = message.DestinationDomain
                },
                AddressInfo = new AddressInfo
                {
                    IpAddress = message.DestinationIpAddress,
                    MacAddress = message.DestinationMacAddress,
                    DnsName = message.DestinationDnsName,
                    Host = message.DestinationHost
                },
                Port = message.DestinationPort,
                End = DateTime.UtcNow,
                GmtDate = message.DestinationGmtDate
            },
            EventObject = message.EventObject ?? string.Empty,
            Message = message.Message ?? string.Empty,
            IsSuccess = message.IsSuccess,
            EventName = message.EventName ?? string.Empty
        };
    }

    private static void SetDestinationDate(AuditEvent auditEvent)
    {
        if (auditEvent.Destination.GmtDate == DateTime.MinValue)
        {
            auditEvent.Destination.GmtDate = DateTime.UtcNow;
        }

        if (auditEvent.Destination.End == DateTime.MinValue)
        {
            auditEvent.Destination.End = DateTime.UtcNow;
        }
    }
}
