using System.Transactions;

namespace Dex.Cap.Common.Ef;

public interface IEfTransactionScopeOptions : IEfTransactionOptions
{
    TransactionScopeOption TransactionScopeOption { get; }
}