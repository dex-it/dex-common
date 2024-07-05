using System.Text.Json.Serialization;
using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Comands.EFCore.DeleteUser;

public class DeleteUserCommand : IAuditRequest<DeleteUserResponse>
{
    [JsonIgnore]
    public string EventType { get; } = nameof(DeleteUserCommand);

    [JsonIgnore]
    public string EventObject { get; } = nameof(DeleteUserCommand);

    [JsonIgnore]
    public string Message { get; } = "User deleted";

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }
}