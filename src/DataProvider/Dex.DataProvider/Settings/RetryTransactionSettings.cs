using System;

namespace Dex.DataProvider.Settings
{
    public sealed class RetryTransactionSettings
    {
        public RetryTransactionSettings(TimeSpan retryDelay)
        {
            RetryDelay = retryDelay;
        }

        public TimeSpan RetryDelay { get; }
    }
}