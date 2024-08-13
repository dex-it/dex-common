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
    /// Получить хэш кода объекта
    /// </summary>
    /// <returns>Хэш сумма</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(IpAddress, MacAddress, DnsName, Host);
    }
}
