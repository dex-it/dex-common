namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об адресе участника системы аудита.
/// </summary>
public class AddressInfo
{
    /// <summary>
    /// IP адрес.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// МАС адрес.
    /// </summary>
    public string? MacAddress { get; set; }

    /// <summary>
    /// DNS-имя.
    /// </summary>
    public string? DnsName { get; set; }

    /// <summary>
    /// Netbios имя или hostname.
    /// </summary>
    public string? Host { get; set; }
}
