namespace Dex.Audit.Logger.Options;

/// <summary>
/// Audit logger options.
/// </summary>
public sealed class AuditLoggerOptions
{
    /// <summary>
    /// The interval of reading the accumulated data.
    /// </summary>
    public TimeSpan ReadEventsInterval { get; set; }
}