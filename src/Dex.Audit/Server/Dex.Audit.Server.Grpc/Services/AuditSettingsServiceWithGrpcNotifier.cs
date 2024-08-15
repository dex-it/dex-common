using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;

namespace Dex.Audit.Server.Grpc.Services;

public class AuditSettingsServiceWithGrpcNotifier(
    IAuditPersistentRepository persistentRepository,
    GrpcAuditServerSettingsService auditServerSettingsService) : IAuditServerSettingsService
{
    public async Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        await persistentRepository.AddOrUpdateSettings(eventType, severityLevel, cancellationToken);
        auditServerSettingsService.NotifyClients();
    }

    public async Task DeleteSettings(string eventType, CancellationToken cancellationToken = default)
    {
        await persistentRepository.DeleteSettings(eventType, cancellationToken);
        auditServerSettingsService.NotifyClients();
    }
}