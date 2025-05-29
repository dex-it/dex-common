using System.Transactions;

namespace Dex.Cap.Common.Ef;

public class EfTransactionOptions : IEfTransactionOptions
{
    public TransactionScopeOption TransactionScopeOption { get; init; } = TransactionScopeOption.Required;

    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.ReadCommitted;

    public uint TimeoutInSeconds { get; init; } = 60;

    public bool ClearChangeTrackerOnRetry { get; init; } = true;

    public static readonly EfTransactionOptions Default = new();

    public static readonly EfTransactionOptions DefaultRepeatableRead =
        new() { IsolationLevel = IsolationLevel.RepeatableRead };

    public static readonly EfTransactionOptions DefaultRequiresNew =
        new() { TransactionScopeOption = TransactionScopeOption.RequiresNew };

    public static readonly EfTransactionOptions DefaultSuppress = new()
        { TransactionScopeOption = TransactionScopeOption.Suppress };
}