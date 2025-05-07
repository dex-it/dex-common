using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using Dex.Audit.Sample.Client.Infrastructure.Context;
using Dex.Audit.Sample.Shared.Enums;
using MediatR;

namespace Dex.Audit.Sample.Client.Application.Commands.Users;

public class UpdateUserHandler(ClientSampleContext context) : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync(request.Id, cancellationToken);

        if (user == null) throw new NullReferenceException();

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Fullname = request.Fullname;

        await context.SaveChangesAsync(cancellationToken);

        return new UpdateUserResponse(user.Id);
    }
}

public sealed class UpdateUserCommand : AuditRequest<UpdateUserResponse>
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

public sealed record UpdateUserResponse(int Id) : IAuditResponse;