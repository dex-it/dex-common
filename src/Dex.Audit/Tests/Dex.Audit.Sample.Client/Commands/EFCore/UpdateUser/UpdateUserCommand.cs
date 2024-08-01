﻿using Dex.Audit.MediatR.Requests;
using Dex.Audit.Sample.Domain.Enums;

namespace Dex.Audit.ClientSample.Commands.EFCore.UpdateUser;

public class UpdateUserCommand : AuditRequest<UpdateUserResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectChanged.ToString();
    public override string EventObject { get; } = nameof(UpdateUserCommand);
    public override string Message { get; } = "User updated";

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