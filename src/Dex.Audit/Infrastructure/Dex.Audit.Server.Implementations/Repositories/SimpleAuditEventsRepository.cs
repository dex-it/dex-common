using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Server.Grpc.Repositories;

/// <summary>
/// Simple implementation of <see cref="IAuditEventsRepository"/>.
/// </summary>
/// <param name="context"><see cref="TDbContext"/></param>
/// <typeparam name="TDbContext"><see cref="DbContext"/> with configured entities of Audit.</typeparam>
public class SimpleAuditEventsRepository<TDbContext>(TDbContext context) : IAuditEventsRepository
    where TDbContext : DbContext
{
    public async Task AddAuditEventsRangeAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        await context.AddRangeAsync(auditEvents, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}