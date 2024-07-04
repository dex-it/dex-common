using System.Data.Common;
using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// Интерсептор для перехвата коммита транзакции в базе данных
/// </summary>
/// <param name="interceptionAndSendingEntriesService">Сервис для перехвата и отправки записей аудита</param>
internal class AuditTransactionInterceptor(IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService) : DbTransactionInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result)
    {
        interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.TransactionCommitting(transaction, eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true).RunSynchronously();
        base.TransactionCommitted(transaction, eventData);
    }

    /// <inheritdoc/>
    public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true, cancellationToken);
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }
}
