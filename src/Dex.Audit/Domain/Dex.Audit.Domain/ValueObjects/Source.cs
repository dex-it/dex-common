namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об источнике события.
/// </summary>
public class Source
{
    /// <summary>
    /// Информация о рабочем устройстве источника события.
    /// </summary>
    public required Device Device { get; init; }

    /// <summary>
    /// Информация о пользователе, инициировавшем событие.
    /// </summary>
    public required UserDetails UserDetails { get; init; }

    /// <summary>
    /// Адрес источника события.
    /// </summary>
    public required AddressInfo AddressInfo { get; init; }

    /// <summary>
    /// Порт на стороне источника.
    /// </summary>
    public string? Port { get; init; }

    /// <summary>
    /// Протокол на стороне источника.
    /// </summary>
    public string? Protocol { get; init; }

    /// <summary>
    ///  Системное время источника события.
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    /// Время GMT источника события.
    /// </summary>
    public DateTime GmtDate { get; init; }

    /// <summary>
    /// Получить хэш кода объекта
    /// </summary>
    /// <returns>Хэш сумма</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Device, UserDetails, AddressInfo, Port, Protocol, Start, GmtDate);
    }
}
