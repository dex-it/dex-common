using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Infrastructure.Repositories;

public class AuditPersistentRepository(AuditServerDbContext context) : IAuditPersistentRepository
{
    public async Task AddAuditEventsRangeAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        await context.AddRangeAsync(auditEvents, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel, CancellationToken cancellationToken = default)
    {
        var setting = await context.AuditSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.EventType == eventType, cancellationToken);

        if (setting == null)
        {
            setting = new AuditSettings { EventType = eventType, SeverityLevel = severityLevel };
        }
        else
        {
            setting.SeverityLevel = severityLevel;
        }

        context.Update(setting);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSettings(string eventType, CancellationToken cancellationToken = default)
    {
        await context.AuditSettings
            .Where(settings => settings.EventType == eventType)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await context.AuditSettings.AsNoTracking().ToListAsync(cancellationToken);
    }
}