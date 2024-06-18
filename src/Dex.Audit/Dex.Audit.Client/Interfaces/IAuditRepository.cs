using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Interfaces;

public interface IAuditRepository
{
    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<List<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken);
}