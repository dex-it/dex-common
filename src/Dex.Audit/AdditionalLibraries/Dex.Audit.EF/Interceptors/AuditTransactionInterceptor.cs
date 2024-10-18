using System.Data.Common;
using Dex.Audit.EF.Extensions;
using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// An interceptor for intercepting a transaction commit in the database.
/// </summary>
/// <param name="interceptionAndSendingEntriesService">A service for intercepting and sending audit records.</param>
internal class AuditTransactionInterceptor(
    IInterceptionAndSendingEntriesService interceptionAndSendingEntriesService)
    : DbTransactionInterceptor, IAuditDbTransactionInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult TransactionCommitting(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result)
    {
        eventData.InterceptIfPossible(interceptionAndSendingEntriesService);
        return base.TransactionCommitting(transaction, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        eventData.InterceptIfPossible(interceptionAndSendingEntriesService);
        return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override void TransactionCommitted(
        DbTransaction transaction,
        TransactionEndEventData eventData)
    {
        interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true).RunSynchronously();
        base.TransactionCommitted(transaction, eventData);
    }

    /// <inheritdoc/>
    public override async Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await interceptionAndSendingEntriesService.SendInterceptedEntriesAsync(true, cancellationToken)
            .ConfigureAwait(false);
        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken).ConfigureAwait(false);
    }
}