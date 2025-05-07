using Dex.Audit.EF.Interceptors.Interceptors;
using Dex.Audit.Sample.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Sample.Client.Infrastructure.Context.Interceptors;

internal class CustomInterceptionAndSendingEntriesService(IServiceProvider serviceProvider) : InterceptionAndSendingEntriesService(serviceProvider)
{
    protected override string GetEventType(EntityState entityState)
    {
        var eventType = entityState switch
        {
            EntityState.Modified => AuditEventType.ObjectChanged.ToString(),
            EntityState.Added => AuditEventType.ObjectCreated.ToString(),
            EntityState.Deleted => AuditEventType.ObjectDeleted.ToString(),
            _ => "None"
        };

        return eventType;
    }
}