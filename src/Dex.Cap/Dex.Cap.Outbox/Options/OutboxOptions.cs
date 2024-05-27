using System;

namespace Dex.Cap.Outbox.Options
{
    public class OutboxOptions
    {
        /// <summary>
        /// Count of retries, to process outbox messages (transient errors).
        /// Default: 3
        /// </summary>
        public int Retries { get; set; } = 3;

        /// <summary>
        /// Count of messages, fetched and locked by the OutboxHandler from database, by the one cycle
        /// Please note that the time to process ALL selected messages will begin from the moment they are selected.
        /// Default: 1
        /// </summary>
        public int MessagesToProcess { get; set; } = 1;

        /// <summary>
        /// Degree of parallel executions. Recommendation ConcurrencyLimit must be less or equal MessagesToProcess.
        /// Default: 1
        /// </summary>
        public int ConcurrencyLimit { get; set; } = 1;

        /// <summary>
        /// Timeout for extract free messages from storage.
        /// Default: 20sec
        /// </summary>
        public TimeSpan GetFreeMessagesTimeout { get; set; } = TimeSpan.FromSeconds(20);
    }
}