namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об источнике события.
/// </summary>
public class Source
{
    /// <summary>
    /// Информация о рабочем устройстве источника события.
    /// </summary>
    public Device Device { get; init; }

    /// <summary>
    /// Информация о пользователе, инициировавшем событие.
    /// </summary>
    public UserDetails UserDetails { get; init; }

    /// <summary>
    /// Адрес источника события.
    /// </summary>
    public AddressInfo AddressInfo { get; init; }

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
    /// Метод сравнения объектов.
    /// </summary>
    /// <param name="obj">Входной объект.</param>
    /// <returns>true, если объекты равны, иначе false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Source source)
        {
            return false;
        }

        return UserDetails.Equals(source.UserDetails) &&
               AddressInfo.Equals(source.AddressInfo) &&
               Port == source.Port &&
               Protocol == source.Protocol &&
               Start == source.Start &&
               GmtDate == source.GmtDate;
    }

    /// <summary>
    /// Метод получения хеш-кода объекта.
    /// </summary>
    /// <returns>Хеш-код на основе свойтв объекта.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(UserDetails, AddressInfo, Port, Protocol, Start, GmtDate);
    }
}
