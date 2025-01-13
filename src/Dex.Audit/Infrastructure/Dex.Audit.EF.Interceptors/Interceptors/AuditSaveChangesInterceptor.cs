using Dex.Audit.EF.Interceptors.Extensions;
using Dex.Audit.EF.Interceptors.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors.Interceptors;

/// <summary>
/// An audit interceptor responsible for intercepting database context changes and sending audit records.
/// </summary>
/// <param name="interceptionAndSendingEntriesService">A service for intercepting and sending audit records.</param>
internal sealed class AuditSaveChangesInterceptor(
    IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService)
    : SaveChangesInterceptor, IAuditSaveChangesInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        eventData.InterceptIfPossible(interceptionAndSendingEntriesService);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        eventData.InterceptIfPossible(interceptionAndSendingEntriesService);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (CheckIfTransactionExists(eventData.Context))
        {
            await interceptionAndSendingEntriesService
                .SendInterceptedEntriesAsync(true, cancellationToken)
                .ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (CheckIfTransactionExists(eventData.Context))
        {
            await interceptionAndSendingEntriesService
                .SendInterceptedEntriesAsync(false, cancellationToken).ConfigureAwait(false);
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken).ConfigureAwait(false);
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
