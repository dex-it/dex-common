using Dex.Audit.Domain.Core;

namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация о пользователе.
/// </summary>
public class UserDetails : BaseValueObject
{
    /// <summary>
    ///  Системное имя (логин) пользователя.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя.
    /// </summary>
    public string? UserDomain { get; init; }
}