namespace Dex.Audit.Domain.Models.AuditEvent;

/// <summary>
/// Информация о получателе объекта события аудита.
/// </summary>
public class Destination
{
    /// <summary>
    /// Информация о пользователе-получателе.
    /// </summary>
    public UserDetails UserDetails { get; set; }

    /// <summary>
    /// Адрес получателя объекта события.
    /// </summary>
    public AddressInfo AddressInfo { get; set; }
    
    /// <summary>
    /// Порт на стороне получателя.
    /// </summary>
    public string? Port { get; set; }
    
    /// <summary>
    /// Системное время получателя.
    /// </summary>
    public DateTime End { get; set; }
    
    /// <summary>
    /// Время GMT получателя.
    /// </summary>
    public DateTime GmtDate { get; set; }
}
