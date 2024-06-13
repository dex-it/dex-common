namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация о пользователе.
/// </summary>
public class UserDetails
{
    /// <summary>
    ///  Системное имя (логин) пользователя.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя.
    /// </summary>
    public string? UserDomain { get; set; }
}