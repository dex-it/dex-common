using System;

namespace Dex.DataProvider.Contracts
{
    public interface IRetryTransactionSettings
    {
        /// <summary>
        /// Delay time before retry transaction.
        /// </summary>
        TimeSpan RetryDelay { get; }
    }
}