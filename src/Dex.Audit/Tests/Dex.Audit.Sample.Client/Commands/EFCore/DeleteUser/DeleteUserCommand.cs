using Dex.Audit.MediatR.Requests;
using Dex.Audit.Sample.Domain.Enums;

namespace Dex.Audit.ClientSample.Commands.EFCore.DeleteUser;

public class DeleteUserCommand : AuditRequest<DeleteUserResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectDeleted.ToString();
    public override string EventObject { get; } = nameof(DeleteUserCommand);
    public override string Message { get; } = "User deleted";

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }
}