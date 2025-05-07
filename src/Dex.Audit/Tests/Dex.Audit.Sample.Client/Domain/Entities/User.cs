using Dex.Audit.EF.Interceptors.Abstractions;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Dex.Audit.Sample.Client.Domain.Entities;

/// <summary>
/// Пользователь системы
/// </summary>
public sealed class User : IAuditEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// ФИО пользователя.
    /// </summary>
    public string? Fullname { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Адрес e-mail.
    /// </summary>
    public string? Email { get; set; }
}
