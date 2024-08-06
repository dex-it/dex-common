using Dex.Audit.EF.Interceptors;
using Dex.Audit.Sample.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ClientSample.Infrastructure.Context.Interceptors;

public class CustomInterceptionAndSendingEntriesService(IServiceProvider serviceProvider) : InterceptionAndSendingEntriesService(serviceProvider)
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