using Dex.Audit.EF.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ClientSample.Context.Interceptors;

public class CustomInterceptionAndSendingEntriesService(IServiceProvider serviceProvider) : InterceptionAndSendingEntriesService(serviceProvider)
{
    protected override string GetEventType(EntityState entityState)
    {
        var eventType = entityState switch
        {
            EntityState.Modified => "EntityUpdated",
            EntityState.Added => "EntityCreated",
            EntityState.Deleted => "EntityDeleted",
            _ => "None"
        };

        return eventType;
    }
}