namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Information about the event source.
/// </summary>
public sealed class Source
{
    /// <summary>
    /// Information about the working device of the event source.
    /// </summary>
    public required Device Device { get; init; }

    /// <summary>
    /// Information about the user who initiated the event.
    /// </summary>
    public required UserDetails UserDetails { get; init; }

    /// <summary>
    /// Address of the event source.
    /// </summary>
    public required AddressInfo AddressInfo { get; init; }

    /// <summary>
    /// Port on the source side.
    /// </summary>
    public string? Port { get; init; }

    /// <summary>
    /// Protocol on the source side.
    /// </summary>
    public string? Protocol { get; init; }

    /// <summary>
    /// System time of the event source.
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    /// GMT time of the event source.
    /// </summary>
    public DateTime GmtDate { get; init; }

    /// <summary>
    /// Get the object's hash code
    /// </summary>
    /// <returns>Hash sum</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Device, UserDetails, AddressInfo, Port, Protocol, Start, GmtDate);
    }
}