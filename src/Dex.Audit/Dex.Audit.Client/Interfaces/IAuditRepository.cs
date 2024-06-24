using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Interfaces;

public interface IAuditRepository
{
    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken);
}