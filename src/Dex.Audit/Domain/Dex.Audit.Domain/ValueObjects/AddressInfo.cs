namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Information about the address of an audit system participant.
/// </summary>
public sealed class AddressInfo
{
    /// <summary>
    /// IP address.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// MAC address.
    /// </summary>
    public string? MacAddress { get; init; }

    /// <summary>
    /// DNS name.
    /// </summary>
    public string? DnsName { get; init; }

    /// <summary>
    /// NetBIOS name or hostname.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Get the object's hash code
    /// </summary>
    /// <returns>Hash sum</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(IpAddress, MacAddress, DnsName, Host);
    }
}