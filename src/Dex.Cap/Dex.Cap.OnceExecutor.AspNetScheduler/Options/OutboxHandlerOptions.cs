using System;

namespace Dex.Cap.OnceExecutor.AspNetScheduler.Options;

internal sealed class OnceExecutorHandlerOptions
{
    /// <summary>
    /// Cleanup processed transactions period
    /// Default 1h
    /// </summary>
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Cleanup transactions older some days
    /// Default 180d
    /// </summary>
    public TimeSpan CleanupOlderThan { get; init; } = TimeSpan.FromDays(180);
}