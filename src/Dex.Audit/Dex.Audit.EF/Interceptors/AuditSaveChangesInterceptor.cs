using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// Интерсептор аудита, отвечающий за перехват изменений контекста базы данных и отправку записей аудита
/// </summary>
internal class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IInterceptionAndSendingEntriesService _interceptionAndSendingEntriesService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditSaveChangesInterceptor"/>.
    /// </summary>
    /// <param name="interceptionAndSendingEntriesService">Сервис для перехвата и отправки записей аудита</param>
    public AuditSaveChangesInterceptor(IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService)
    {
        _interceptionAndSendingEntriesService = interceptionAndSendingEntriesService;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        _interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (CheckTransaction(eventData.Context))
        {
            await _interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (CheckTransaction(eventData.Context))
        {
            await _interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(false, cancellationToken);
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static bool CheckTransaction(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return false;
        }

        return dbContext.Database.CurrentTransaction is null;
    }
}
