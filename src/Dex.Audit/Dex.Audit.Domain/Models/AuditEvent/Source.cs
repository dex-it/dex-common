namespace Dex.Audit.Domain.Models.AuditEvent;

/// <summary>
/// Информация об источнике события.
/// </summary>
public class Source
{
    /// <summary>
    /// Информация о рабочем устройстве источника события.
    /// </summary>
    public Device Device { get; set; }

    /// <summary>
    /// Информация о пользователе, инициировавшем событие.
    /// </summary>
    public UserDetails UserDetails { get; set; }

    /// <summary>
    /// Адрес источника события.
    /// </summary>
    public AddressInfo AddressInfo { get; set; }

    /// <summary>
    /// Порт на стороне источника.
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// Протокол на стороне источника.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    ///  Системное время источника события.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Время GMT источника события.
    /// </summary>
    public DateTime GmtDate { get; set; }
}
