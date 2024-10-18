namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Information about the recipient of the audit event object.
/// </summary>
public class Destination
{
    /// <summary>
    /// Information about the recipient user.
    /// </summary>
    public required UserDetails UserDetails { get; init; }

    /// <summary>
    /// Address of the recipient of the event object.
    /// </summary>
    public required AddressInfo AddressInfo { get; init; }

    /// <summary>
    /// Port on the recipient's side.
    /// </summary>
    public string? Port { get; init; }

    /// <summary>
    /// System time of the recipient.
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// GMT time of the recipient.
    /// </summary>
    public DateTime GmtDate { get; set; }

    /// <summary>
    /// Get the object's hash code
    /// </summary>
    /// <returns>Hash sum</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(UserDetails, AddressInfo, Port);
    }
}