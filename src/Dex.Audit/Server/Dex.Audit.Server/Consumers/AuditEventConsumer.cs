using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.ValueObjects;
using Dex.Audit.Server.Abstractions.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Consumers;

/// <summary>
/// Consumer for audit events received via the message bus.
/// </summary>
/// <param name="logger"><see cref="ILogger"/>.</param>
/// <param name="auditEventsRepository"><see cref="IAuditEventsRepository"/>.</param>
/// <param name="auditSettingsCacheRepository"><see cref="IAuditSettingsCacheRepository"/>.</param>
public class AuditEventConsumer(
    ILogger<AuditEventConsumer> logger,
    IAuditEventsRepository auditEventsRepository,
    IAuditSettingsCacheRepository auditSettingsCacheRepository)
    : IConsumer<Batch<AuditEventMessage>>
{
    /// <summary>
    /// A method for processing audit events received via the message bus.
    /// </summary>
    /// <param name="context">The context of the message containing the audit event to be processed.</param>
    public async Task Consume(
        ConsumeContext<Batch<AuditEventMessage>> context)
    {
        try
        {
            var unMappedAuditEvents = await GetRelevantAuditMessages(context, context.CancellationToken)
                .ConfigureAwait(false);

            var auditEvents = unMappedAuditEvents.Select(MapAuditEventFromMessage);

            await auditEventsRepository
                .AddAuditEventsRangeAsync(auditEvents, context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing audit messages.");
        }
    }

    private async Task<IEnumerable<AuditEventMessage>> GetRelevantAuditMessages(
        ConsumeContext<Batch<AuditEventMessage>> context,
        CancellationToken cancellationToken)
    {
        var auditMessages = context.Message
            .Where(consumeContext => consumeContext.Message.AuditSettingsId != null)
            .Select(consumeContext => consumeContext.Message)
            .ToList();

        var messagesWithUnknownSettings = context.Message
            .Where(consumeContext => consumeContext.Message.AuditSettingsId is null)
            .Select(consumeContext => consumeContext.Message)
            .ToArray();

        var auditSettings = await GetAuditSettings(messagesWithUnknownSettings, cancellationToken);

        foreach (var message in messagesWithUnknownSettings)
        {
            var eventType = message.EventType;
            var sourceIp = message.SourceIpAddress;

            auditSettings.TryGetValue(eventType, out var setting);

            if (setting == null)
            {
                logger.LogWarning("Failed to retrieve settings for event type [{EventType}] from cache.", eventType);

                continue;
            }

            if (setting.SeverityLevel >= message.SourceMinSeverityLevel)
            {
                auditMessages.Add(message);

                continue;
            }

            logger.LogWarning(
                "The minimum source level [{SourceSeverityLevel}] specified when sending the audit message [{EventType}] from [{SourceIp}] does not match the actual one in the cache [{CurrentSeverityLevel}].",
                message.SourceMinSeverityLevel,
                eventType,
                sourceIp,
                setting.SeverityLevel);
        }

        return auditMessages;
    }

    private Task<IDictionary<string, AuditSettings?>> GetAuditSettings(
        AuditEventMessage[] messagesWithUnknownSettings,
        CancellationToken cancellationToken)
    {
        var unknownEventTypes = messagesWithUnknownSettings
            .Select(pair => pair.EventType)
            .Distinct()
            .ToArray();

        return auditSettingsCacheRepository.GetDictionaryAsync(unknownEventTypes, cancellationToken);
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