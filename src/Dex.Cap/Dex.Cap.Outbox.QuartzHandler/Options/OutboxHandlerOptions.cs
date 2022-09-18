using System;

namespace Dex.Cap.Outbox.AspNetScheduler.Options
{
    internal sealed class OutboxHandlerOptions
    {
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan CleanupOlderThan { get; set; } = TimeSpan.FromDays(30);
    }
}
