using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.Interfaces;
using Dex.Audit.ServerSample.Context;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Repositories;

public class AuditRepository(AuditServerDbContext serverDbContext) : IAuditRepository
{
    public async Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await serverDbContext.AddAsync(auditEvent, cancellationToken);
        await serverDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await serverDbContext.AuditSettings.ToListAsync(cancellationToken);
    }
}