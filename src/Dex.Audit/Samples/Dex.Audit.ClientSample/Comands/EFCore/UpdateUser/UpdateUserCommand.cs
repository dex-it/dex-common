using System.Text.Json.Serialization;
using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Comands.EFCore.UpdateUser;

public class UpdateUserCommand : IAuditRequest<UpdateUserResponse>
{
    [JsonIgnore]
    public string EventType { get; } = nameof(UpdateUserCommand);

    [JsonIgnore]
    public string EventObject { get; } = nameof(UpdateUserCommand);

    [JsonIgnore]
    public string Message { get; } = "User added";

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

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