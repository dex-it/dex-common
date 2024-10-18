using Dex.Audit.Domain.Entities;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.ServerSample.Infrastructure.Context;

namespace Dex.Audit.ServerSample.Infrastructure.Repositories;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once SuggestBaseTypeForParameterInConstructor
internal class AuditEventsRepository(AuditServerDbContext context) : IAuditEventsRepository
{
    public async Task AddAuditEventsRangeAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        await context.AddRangeAsync(auditEvents, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}