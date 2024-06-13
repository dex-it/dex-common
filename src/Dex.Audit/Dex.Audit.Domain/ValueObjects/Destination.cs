namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация о получателе объекта события аудита.
/// </summary>
public class Destination
{
    /// <summary>
    /// Информация о пользователе-получателе.
    /// </summary>
    public UserDetails UserDetails { get; init; }

    /// <summary>
    /// Адрес получателя объекта события.
    /// </summary>
    public AddressInfo AddressInfo { get; init; }
    
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

    /// <summary>
    /// Метод сравнения объектов.
    /// </summary>
    /// <param name="obj">Входной объект.</param>
    /// <returns>true, если объекты равны, иначе false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Destination destination)
        {
            return false;
        }

        return UserDetails.Equals(destination.UserDetails) &&
               AddressInfo.Equals(destination.AddressInfo) &&
               Port == destination.Port &&
               End == destination.End &&
               GmtDate == destination.GmtDate;
    }

    /// <summary>
    /// Метод получения хеш-кода объекта.
    /// </summary>
    /// <returns>Хеш-код на основе свойтв объекта.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(UserDetails, AddressInfo, Port, End, GmtDate);
    }
}
