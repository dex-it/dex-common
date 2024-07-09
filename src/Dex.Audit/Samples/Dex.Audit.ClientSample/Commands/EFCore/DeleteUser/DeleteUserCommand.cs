using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Commands.EFCore.DeleteUser;

public class DeleteUserCommand : AuditRequest<DeleteUserResponse>
{
    public override string Message { get; } = "User deleted";

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }
}