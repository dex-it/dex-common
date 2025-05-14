using System.Transactions;

namespace Dex.Cap.Common.Ef;

public class EfTransactionOptions : IEfTransactionOptions
{
    public TransactionScopeOption TransactionScopeOption { get; set; } = TransactionScopeOption.Required;

    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

    public uint TimeoutInSeconds { get; set; } = 60;
}