using System.Transactions;

namespace Dex.Cap.OnceExecutor.Ef
{
    public class EfOptions : IEfOptions
    {
        public TransactionScopeOption TransactionScopeOption { get; set; } = TransactionScopeOption.Required;

        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        public uint TimeoutInSeconds { get; set; } = 60;
    }
}