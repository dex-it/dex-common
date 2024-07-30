using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.ValueObjects;
using Dex.Audit.Server.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Consumers;

/// <summary>
/// Обработчик аудиторских событий, полученных через шину сообщений.
/// </summary>
/// <param name="auditRepository"><see cref="IAuditRepository"/>.</param>
/// <param name="logger"><see cref="ILogger"/>.</param>
/// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/>.</param>
public class AuditEventConsumer(IAuditRepository auditRepository,
    ILogger<AuditEventConsumer> logger,
    IAuditSettingsRepository auditSettingsRepository) : IConsumer<Batch<AuditEventMessage>>
{
    /// <summary>
    /// Метод для обработки аудиторских событий, полученных через шину сообщений.
    /// </summary>
    /// <param name="context">Контекст сообщения, содержащий аудиторское событие для обработки.</param>
    public async Task Consume(ConsumeContext<Batch<AuditEventMessage>> context)
    {
        try
        {
            var unMappedAuditEvents = await GetRelevantAuditMessages(context).ConfigureAwait(false);

            var auditEvents = unMappedAuditEvents.Select(MapAuditEventFromMessage);

            await auditRepository.AddAuditEventsRangeAsync(auditEvents, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Возникла ошибка при обработке сообщений аудита.");
        }
    }

    private async Task<IEnumerable<AuditEventMessage>> GetRelevantAuditMessages(ConsumeContext<Batch<AuditEventMessage>> context)
    {
        var auditMessages = context.Message.Select(consumeContext => consumeContext.Message).ToArray();

        var auditEventMessages = auditMessages.ToHashSet();

        foreach (var message in auditMessages.Where(consumeContext => consumeContext.AuditSettingsId is null))
        {
            var eventType = message.EventType;
            var sourceIp = message.SourceIpAddress;

            var auditSettings = await auditSettingsRepository.GetAsync(eventType).ConfigureAwait(false);

            if (auditSettings is null)
            {
                logger.LogWarning("Не удалось получить из кэша настройки для события типа [{EventType}]", eventType);

                auditEventMessages.Remove(message);

                continue;
            }

            if (auditSettings.SeverityLevel >= message.SourceMinSeverityLevel)
            {
                continue;
            }

            logger.LogWarning(
                "Минимальный уровень источника [{SourceSeverityLevel}], указанный во время отправки сообщение аудита [{EventType}] от [{SourceIp}] не соответствует актуальному в кэше [{CurrentSeverityLevel}].",
                message.SourceMinSeverityLevel,
                eventType,
                sourceIp,auditSettings.SeverityLevel);

            auditEventMessages.Remove(message);
        }

        return auditEventMessages;
    }

    private static AuditEvent MapAuditEventFromMessage(AuditEventMessage message)
    {
        var auditEvent = new AuditEvent
        {
            EventType = message.EventType,
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
            IsSuccess = message.IsSuccess
        };

        SetDestinationDate(auditEvent);

        return auditEvent;
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