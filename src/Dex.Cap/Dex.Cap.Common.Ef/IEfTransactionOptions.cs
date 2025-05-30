using System.Transactions;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Common.Ef;

public interface IEfTransactionOptions : ITransactionOptions
{
    public TransactionScopeOption TransactionScopeOption { get; init; }

    public IsolationLevel IsolationLevel { get; init; }

    public uint TimeoutInSeconds { get; init; }

    public bool ClearChangeTrackerOnRetry { get; init; }
}