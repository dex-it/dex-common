using System.Transactions;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Common.Ef;

public interface IEfTransactionOptions : ITransactionOptions
{
    IsolationLevel IsolationLevel { get; }

    uint TimeoutInSeconds { get; }

    bool ClearChangeTrackerOnRetry { get; }
}