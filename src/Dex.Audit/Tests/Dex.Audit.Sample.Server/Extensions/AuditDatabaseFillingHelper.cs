using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Shared.Enums;
using Dex.Audit.ServerSample.Infrastructure.Context;

namespace Dex.Audit.ServerSample.Extensions;

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
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.StartSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ShutdownSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectCreated.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectChanged.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectRead.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectDeleted.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                }
            };
        context.AuditSettings.AddRange(settings);
        context.SaveChanges();
    }
}