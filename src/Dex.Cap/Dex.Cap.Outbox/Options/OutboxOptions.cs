using System;

namespace Dex.Cap.Outbox.Options
{
    public class OutboxOptions
    {
        public int Retries { get; set; } = 3;
        public TimeSpan ProcessorDelay { get; set; } = TimeSpan.FromSeconds(1);
        public int MessagesToProcess { get; set; } = 100;
    }
}