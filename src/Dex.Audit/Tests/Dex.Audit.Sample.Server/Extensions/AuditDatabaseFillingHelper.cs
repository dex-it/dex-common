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
        var settings = new AuditSettings
            []
            {
                new()
                {
                    EventType = AuditEventType.None.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.StartSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.ShutdownSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.ObjectCreated.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.ObjectChanged.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.ObjectRead.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                },
                new()
                {
                    EventType = AuditEventType.ObjectDeleted.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.Low
                }
            };
        context.AuditSettings.AddRange(settings);
        context.SaveChanges();
    }
}