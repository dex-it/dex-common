using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using Dex.Audit.Sample.Client.Domain.Entities;
using Dex.Audit.Sample.Client.Infrastructure.Context;
using Dex.Audit.Sample.Shared.Enums;
using MediatR;

namespace Dex.Audit.Sample.Client.Application.Commands.Users;

public class AddUserHandler(ClientSampleContext context) : IRequestHandler<AddUserCommand, AddUserResponse>
{
    public async Task<AddUserResponse> Handle(AddUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { UserName = request.UserName, Email = request.Email, Fullname = request.Fullname };

        await context.Users.AddAsync(user, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return new AddUserResponse(user.Id);
    }
}

public sealed class AddUserCommand : AuditRequest<AddUserResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectCreated.ToString();
    public override string EventObject { get; } = nameof(AddUserCommand);
    public override string Message { get; } = "User added";

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

public sealed record AddUserResponse(int Id) : IAuditResponse;