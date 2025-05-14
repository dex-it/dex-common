using System.Transactions;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Common.Ef;

public interface IEfTransactionOptions : ITransactionOptions
{
    public TransactionScopeOption TransactionScopeOption { get; set; }

    public IsolationLevel IsolationLevel { get; set; }

    public uint TimeoutInSeconds { get; set; }
}