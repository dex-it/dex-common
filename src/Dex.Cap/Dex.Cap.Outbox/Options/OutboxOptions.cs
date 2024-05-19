using System;

namespace Dex.Cap.Outbox.Options
{
    public class OutboxOptions
    {
        /// <summary>
        /// Count of retries, to process outbox messages (transient errors)
        /// </summary>
        public int Retries { get; set; } = 3;
        
        /// <summary>
        /// Delay after every processing cycle completed 
        /// </summary>
        public TimeSpan ProcessorDelay { get; set; } = TimeSpan.FromSeconds(1);
        
        /// <summary>
        /// Count of messages, fetched and locked by the OutboxHandler from database, by the one cycle
        /// Please note that the time to process ALL selected messages will begin from the moment they are selected.
        /// </summary>
        public int MessagesToProcess { get; set; } = 1;
    }
}