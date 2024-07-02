using System.Data.Common;
using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// Интерсептор для перехвата коммита транзакции в базе данных
/// </summary>
internal class AuditTransactionInterceptor : DbTransactionInterceptor
{
    private readonly IInterceptionAndSendingEntriesService _interceptionAndSendingEntriesService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditTransactionInterceptor"/>.
    /// </summary>
    /// <param name="interceptionAndSendingEntriesService">Сервис для перехвата и отправки записей аудита</param>
    public AuditTransactionInterceptor(IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService)
    {
        _interceptionAndSendingEntriesService = interceptionAndSendingEntriesService ??
                                                throw new ArgumentNullException(nameof(interceptionAndSendingEntriesService));
    }

    /// <inheritdoc/>
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result)
    {
        _interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return base.TransactionCommitting(transaction, eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        _interceptionAndSendingEntriesService.InterceptEntries(eventData.Context!.ChangeTracker.Entries());
        return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        _interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true).RunSynchronously();
        base.TransactionCommitted(transaction, eventData);
    }

    /// <inheritdoc/>
    public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await _interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true, cancellationToken);
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }
}
