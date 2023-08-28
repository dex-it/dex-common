using System.Transactions;

namespace Dex.Cap.OnceExecutor.Ef
{
    public interface IEfOptions : IOnceExecutorOptions
    {
        public TransactionScopeOption TransactionScopeOption { get; set; }

        public IsolationLevel IsolationLevel { get; set; }

        public uint TimeoutInSeconds { get; set; }
    }
}