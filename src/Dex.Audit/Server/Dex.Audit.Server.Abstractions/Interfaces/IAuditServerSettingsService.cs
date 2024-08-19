using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

public interface IAuditServerSettingsService
{
    Task AddOrUpdateSettingsAsync(string eventType, AuditEventSeverityLevel severityLevel, CancellationToken cancellationToken = default);

    Task DeleteSettingsAsync(string eventType, CancellationToken cancellationToken = default);
}