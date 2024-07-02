namespace Dex.Audit.Server.Options;

/// <summary>
/// Класс конфигураций используемых хостов сервиса Client API.
/// </summary>
public class AuditCacheOptions
{
    /// <summary>
    /// Интервал для обновления кэша.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; }
}
