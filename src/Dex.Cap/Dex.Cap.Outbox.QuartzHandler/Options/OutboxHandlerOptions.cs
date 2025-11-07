using System;

namespace Dex.Cap.Outbox.AspNetScheduler.Options;

internal sealed class OutboxHandlerOptions
{
    /// <summary>
    /// Period between cycle OutboxHandler
    /// Default 30sec
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cleanup processed messages period
    /// Default 1h
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Cleanup message older some days
    /// Default 30d
    /// </summary>
    public TimeSpan CleanupOlderThan { get; set; } = TimeSpan.FromDays(30);
}