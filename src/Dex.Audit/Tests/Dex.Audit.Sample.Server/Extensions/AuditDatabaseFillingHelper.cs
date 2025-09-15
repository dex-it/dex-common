using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Server.Infrastructure.Context;
using Dex.Audit.Sample.Shared.Enums;

namespace Dex.Audit.Sample.Server.Extensions;

internal static class AuditDatabaseFillingHelper
{
    internal static void FillAuditSettings(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AuditServerDbContext>();
        var settings = new AuditSettings[]
        {
            new()
            {
                EventType = nameof(AuditEventType.None),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.StartSystem),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.ShutdownSystem),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.ObjectCreated),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.ObjectChanged),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.ObjectRead),
                SeverityLevel = AuditEventSeverityLevel.Low
            },
            new()
            {
                EventType = nameof(AuditEventType.ObjectDeleted),
                SeverityLevel = AuditEventSeverityLevel.Low
            }
        };
        context.AuditSettings.AddRange(settings);
        context.SaveChanges();
    }
}