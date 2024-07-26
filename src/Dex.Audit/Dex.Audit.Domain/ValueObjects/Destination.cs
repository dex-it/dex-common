using Dex.Audit.Domain.Core;

namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация о получателе объекта события аудита.
/// </summary>
public class Destination : BaseValueObject
{
    /// <summary>
    /// Информация о пользователе-получателе.
    /// </summary>
    public required UserDetails UserDetails { get; init; }

    /// <summary>
    /// Адрес получателя объекта события.
    /// </summary>
    public required AddressInfo AddressInfo { get; init; }
    
    /// <summary>
    /// Порт на стороне получателя.
    /// </summary>
    public string? Port { get; init; }
    
    /// <summary>
    /// Системное время получателя.
    /// </summary>
    public DateTime End { get; set; }
    
    /// <summary>
    /// Время GMT получателя.
    /// </summary>
    public DateTime GmtDate { get; set; }
}
