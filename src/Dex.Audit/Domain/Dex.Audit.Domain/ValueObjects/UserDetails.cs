namespace Dex.Audit.Domain.ValueObjects;

/// <summary>
/// Информация о пользователе.
/// </summary>
public class UserDetails
{
    /// <summary>
    ///  Системное имя (логин) пользователя.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Домен (или имя рабочей группы) пользователя.
    /// </summary>
    public string? UserDomain { get; init; }

    /// <summary>
    /// Получить хэш кода объекта
    /// </summary>
    /// <returns>Хэш сумма</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(User, UserDomain);
    }
}