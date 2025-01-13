namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Information about the user.
/// </summary>
public sealed class UserDetails
{
    /// <summary>
    /// System username (login).
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Domain (or workgroup name) of the user.
    /// </summary>
    public string? UserDomain { get; init; }

    /// <summary>
    /// Get the object's hash code
    /// </summary>
    /// <returns>Hash sum</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(User, UserDomain);
    }
}