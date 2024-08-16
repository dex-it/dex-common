using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;

namespace Dex.Audit.ServerSample.Servers;

public class AuditServerSettingsService : IAuditServerSettingsService
{
    public Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteSettings(string eventType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}