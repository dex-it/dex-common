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
    /// Метод сравнения объектов.
    /// </summary>
    /// <param name="obj">Входной объект.</param>
    /// <returns>true, если объекты равны, иначе false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not UserDetails userDetails)
        {
            return false;
        }

        return User == userDetails.User &&
               UserDomain == userDetails.UserDomain;
    }

    /// <summary>
    /// Метод получения хеш-кода объекта.
    /// </summary>
    /// <returns>Хеш-код на основе свойтв объекта.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(User, UserDomain);
    }
}