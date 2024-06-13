namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об адресе участника системы аудита.
/// </summary>
public class AddressInfo
{
    /// <summary>
    /// IP адрес.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// МАС адрес.
    /// </summary>
    public string? MacAddress { get; init; }

    /// <summary>
    /// DNS-имя.
    /// </summary>
    public string? DnsName { get; init; }

    /// <summary>
    /// Netbios имя или hostname.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Метод сравнения объектов.
    /// </summary>
    /// <param name="obj">Входной объект.</param>
    /// <returns>true, если объекты равны, иначе false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not AddressInfo addressInfo)
        {
            return false;
        }

        return IpAddress == addressInfo.IpAddress &&
               MacAddress == addressInfo.MacAddress && 
               DnsName == addressInfo.DnsName && 
               Host == addressInfo.Host;
    }

    /// <summary>
    /// Метод получения хеш-кода объекта.
    /// </summary>
    /// <returns>Хеш-код на основе свойтв объекта.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(IpAddress, MacAddress, DnsName, Host);
    }
}
