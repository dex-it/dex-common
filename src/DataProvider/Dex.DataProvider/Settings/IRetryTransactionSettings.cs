using System;

namespace Dex.DataProvider.Settings
{
    public interface IRetryTransactionSettings
    {
        TimeSpan RetryDelay { get; }
    }
}