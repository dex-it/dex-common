using Dex.Audit.EF.Interfaces;

namespace Dex.Audit.ClientSample.Domain.Entities;

/// <summary>
/// Пользователь системы
/// </summary>
public class User : IAuditEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

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
