namespace Dex.Audit.ClientSample.Queries.GetUserQuery;

public class GetUserQueryResponse
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