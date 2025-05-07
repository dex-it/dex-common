using Dex.Audit.Domain.Enums;
using Dex.Audit.Implementations.Common.Dto;
using Dex.Audit.Server.Abstractions.Interfaces;
using MassTransit;

namespace Dex.Audit.Server.Implementations.Services;

/// <summary>
/// Simple implementation of <see cref="IAuditServerSettingsService"/>.
/// </summary>
/// <param name="auditSettingsRepository"><see cref="IAuditSettingsRepository"/></param>
/// <param name="publishEndpoint"><see cref="IPublishEndpoint"/></param>
public class SimpleAuditServerSettingsService(
    IAuditSettingsRepository auditSettingsRepository,
    IPublishEndpoint publishEndpoint)
    : IAuditServerSettingsService
{
    public async Task AddOrUpdateSettingsAsync(string eventType, AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        await auditSettingsRepository
            .AddOrUpdateSettings(eventType, severityLevel, cancellationToken)
            .ConfigureAwait(false);

        await SendSettingsUpdated(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        await auditSettingsRepository
            .DeleteSettings(eventType, cancellationToken)
            .ConfigureAwait(false);

        await SendSettingsUpdated(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SendSettingsUpdated(CancellationToken cancellationToken)
    {
        var settings = await auditSettingsRepository
            .GetAllSettingsAsync(cancellationToken)
            .ConfigureAwait(false);

        await publishEndpoint
            .Publish(new AuditSettingsDto(settings
                .Select(auditSettings => new AuditSettingDto(auditSettings.Id, auditSettings.EventType, auditSettings.SeverityLevel))),
                cancellationToken)
            .ConfigureAwait(false);
    }
}