using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;

namespace Dex.Audit.Server.Grpc.Services;

internal class AuditSettingsServiceWithGrpcNotifier(
    IAuditPersistentRepository persistentRepository,
    GrpcAuditServerSettingsService auditServerSettingsService)
    : IAuditServerSettingsService
{
    public async Task AddOrUpdateSettingsAsync(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        await persistentRepository
            .AddOrUpdateSettings(eventType, severityLevel, cancellationToken)
            .ConfigureAwait(false);
        auditServerSettingsService.NotifyClients();
    }

    public async Task DeleteSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        await persistentRepository
            .DeleteSettings(eventType, cancellationToken)
            .ConfigureAwait(false);
        auditServerSettingsService.NotifyClients();
    }
}