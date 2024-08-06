using Dex.Audit.ClientSample.Infrastructure.Context;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using Dex.Audit.Sample.Shared.Enums;
using MediatR;

namespace Dex.Audit.ClientSample.Application.Commands.Users;

public class DeleteUserHandler(ClientSampleContext context) : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync(request.Id, cancellationToken);

        if (user == null) throw new NullReferenceException();

        context.Users.Remove(user);

        await context.SaveChangesAsync(cancellationToken);

        return new DeleteUserResponse();
    }
}

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

public class DeleteUserResponse : IAuditResponse;