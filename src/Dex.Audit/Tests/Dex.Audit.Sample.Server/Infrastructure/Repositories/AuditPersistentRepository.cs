using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Infrastructure.Repositories;

public class AuditPersistentRepository(AuditServerDbContext serverDbContext) : IAuditPersistentRepository
{
    public async Task AddAuditEventsRangeAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        await serverDbContext.AddRangeAsync(auditEvents, cancellationToken);
        await serverDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel, CancellationToken cancellationToken = default)
    {
        var settings =
            await serverDbContext.AuditSettings.FirstOrDefaultAsync(auditSettings => auditSettings.EventType == eventType, cancellationToken);
        settings.SeverityLevel = severityLevel;
        serverDbContext.Update(settings);
        await serverDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSettings(string eventType, CancellationToken cancellationToken = default)
    {
        await serverDbContext.AuditSettings
            .Where(settings => settings.EventType == eventType)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await serverDbContext.AuditSettings.ToListAsync(cancellationToken);
    }
}