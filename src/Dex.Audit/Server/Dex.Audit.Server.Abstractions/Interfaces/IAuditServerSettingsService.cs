using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

public interface IAuditServerSettingsService
{
    Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel, CancellationToken cancellationToken = default);

    Task DeleteSettings(string eventType, CancellationToken cancellationToken = default);
}