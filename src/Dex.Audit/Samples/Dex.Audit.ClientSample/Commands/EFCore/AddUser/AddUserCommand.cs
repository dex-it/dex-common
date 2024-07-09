using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Commands.EFCore.AddUser;

public class AddUserCommand : AuditRequest<AddUserResponse>
{
    public override string Message { get; } = "User added";

    /// <summary>
    /// ФИО пользователя.
    /// </summary>
    public string Fullname { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Адрес e-mail.
    /// </summary>
    public string Email { get; set; }
}