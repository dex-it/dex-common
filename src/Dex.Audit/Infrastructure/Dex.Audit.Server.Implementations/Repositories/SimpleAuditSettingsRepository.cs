using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Server.Grpc.Repositories;

/// <summary>
/// Simple implementation of <see cref="IAuditSettingsRepository"/>.
/// </summary>
/// <param name="context"><see cref="TDbContext"/></param>
/// <typeparam name="TDbContext"><see cref="DbContext"/> with configured entities of Audit.</typeparam>
public class SimpleAuditSettingsRepository<TDbContext>(TDbContext context) : IAuditSettingsRepository
    where TDbContext : DbContext
{
    public async Task AddOrUpdateSettings(string eventType, AuditEventSeverityLevel severityLevel, CancellationToken cancellationToken = default)
    {
        var setting = await context
            .Set<AuditSettings>()
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
        await context.Set<AuditSettings>()
            .Where(settings => settings.EventType == eventType)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await context
            .Set<AuditSettings>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}