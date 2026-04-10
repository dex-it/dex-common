using System.Transactions;

namespace Dex.Cap.Common.Ef;

public class EfTransactionScopeOptions : EfTransactionOptions, IEfTransactionScopeOptions
{
    public TransactionScopeOption TransactionScopeOption { get; init; } = TransactionScopeOption.Required;

    public new static readonly EfTransactionScopeOptions Default = new();

    public new static readonly EfTransactionScopeOptions DefaultRepeatableRead = new() { IsolationLevel = IsolationLevel.RepeatableRead };

    public static readonly EfTransactionScopeOptions DefaultRequiresNew = new() { TransactionScopeOption = TransactionScopeOption.RequiresNew };

    public static readonly EfTransactionScopeOptions DefaultSuppress = new() { TransactionScopeOption = TransactionScopeOption.Suppress };
}