using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Extensions;

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