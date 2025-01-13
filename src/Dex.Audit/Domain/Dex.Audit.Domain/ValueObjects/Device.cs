namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Information about the device of an audit system participant.
/// </summary>
public sealed class Device
{
    /// <summary>
    /// Name of the information system (IS).
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Version of the IS.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Name of the IS audit log.
    /// </summary>
    public string? Product { get; init; }

    /// <summary>
    /// Full name of the process (service).
    /// </summary>
    public string? ProcessName { get; init; }

    /// <summary>
    /// System identifier of the event message.
    /// </summary>
    public long? EventClassId { get; init; }

    /// <summary>
    /// Get the object's hash code
    /// </summary>
    /// <returns>Hash sum</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Vendor, Version, Product, ProcessName, EventClassId);
    }
}