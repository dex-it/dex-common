using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// Интерсептор аудита, отвечающий за перехват изменений контекста базы данных и отправку записей аудита
/// </summary>
/// <param name="interceptionAndSendingEntriesService">Сервис для перехвата и отправки записей аудита</param>
internal class AuditSaveChangesInterceptor(IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService) : SaveChangesInterceptor, IAuditSaveChangesInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (CheckIfTransactionExists(eventData.Context))
        {
            await interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (CheckIfTransactionExists(eventData.Context))
        {
            await interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(false, cancellationToken);
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static bool CheckIfTransactionExists(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return false;
        }

        return dbContext.Database.CurrentTransaction is null;
    }
}
