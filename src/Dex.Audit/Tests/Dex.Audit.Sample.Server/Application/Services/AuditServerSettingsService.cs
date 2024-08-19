using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Shared.Dto;
using Dex.Audit.Server.Abstractions.Interfaces;
using MassTransit;

namespace Dex.Audit.ServerSample.Application.Services;

public class AuditServerSettingsService(IAuditPersistentRepository persistentRepository, IPublishEndpoint publishEndpoint) : IAuditServerSettingsService
{
    public async Task AddOrUpdateSettingsAsync(string eventType, AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        await persistentRepository.AddOrUpdateSettings(eventType, severityLevel, cancellationToken);

        await SendSettingsUpdated(cancellationToken);
    }

    public async Task DeleteSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        await persistentRepository.DeleteSettings(eventType, cancellationToken);

        await SendSettingsUpdated(cancellationToken);
    }

    private async Task SendSettingsUpdated(CancellationToken cancellationToken)
    {
        var settings = await persistentRepository.GetAllSettingsAsync(cancellationToken);

        await publishEndpoint
            .Publish(new AuditSettingsDto(settings
                .Select(auditSettings => new AuditSettingDto(auditSettings.Id, auditSettings.EventType, auditSettings.SeverityLevel))),
                cancellationToken);
    }
}