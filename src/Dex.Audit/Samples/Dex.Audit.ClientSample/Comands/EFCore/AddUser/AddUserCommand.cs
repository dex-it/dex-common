using System.Text.Json.Serialization;
using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Comands.EFCore.AddUser;

public class AddUserCommand : IAuditRequest<AddUserResponse>
{
    [JsonIgnore]
    public string EventType { get; } = nameof(AddUserCommand);

    [JsonIgnore]
    public string EventObject { get; } = nameof(AddUserCommand);

    [JsonIgnore]
    public string Message { get; } = "User added";

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