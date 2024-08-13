namespace Dex.Audit.Logger.Options;

/// <summary>
/// Опции аудиремого логгера.
/// </summary>
public class AuditLoggerOptions
{
    /// <summary>
    /// Интервал прочтения накопленных собитий.
    /// </summary>
    public TimeSpan ReadEventsInterval { get; set; }
}