using Dex.Audit.EF.Interceptors.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors.Extensions;

internal static class DbContextEventDataExtensions
{
    internal static void InterceptIfPossible(this DbContextEventData eventData, IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService)
    {
        if (eventData.Context != null)
        {
            interceptionAndSendingEntriesService.InterceptEntries(eventData.Context.ChangeTracker.Entries());
        }
    }
}