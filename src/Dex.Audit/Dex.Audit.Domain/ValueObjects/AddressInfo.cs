using Dex.Audit.Domain.Core;

namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация об адресе участника системы аудита.
/// </summary>
public class AddressInfo : BaseValueObject
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
}
