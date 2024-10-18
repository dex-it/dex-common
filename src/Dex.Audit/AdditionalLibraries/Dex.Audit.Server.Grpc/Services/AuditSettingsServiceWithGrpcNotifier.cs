using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;

namespace Dex.Audit.Server.Grpc.Services;

internal class AuditSettingsServiceWithGrpcNotifier(
    IAuditSettingsRepository eventsRepository,
    GrpcAuditServerSettingsService auditServerSettingsService)
    : IAuditServerSettingsService
{
    public async Task AddOrUpdateSettingsAsync(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        await eventsRepository
            .AddOrUpdateSettings(eventType, severityLevel, cancellationToken)
            .ConfigureAwait(false);
        auditServerSettingsService.NotifyClients();
    }

    public async Task DeleteSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        await eventsRepository
            .DeleteSettings(eventType, cancellationToken)
            .ConfigureAwait(false);
        auditServerSettingsService.NotifyClients();
    }
}