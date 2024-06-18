namespace Dex.Audit.Client.Options;

/// <summary>
/// Класс конфигураций используемых хостов сервиса Client API.
/// </summary>
public class AuditCacheOptions
{
    /// <summary>
    /// Интервал для обновления кэша (в минутах)
    /// </summary>
    public TimeSpan RefreshInterval { get; set; }
}
