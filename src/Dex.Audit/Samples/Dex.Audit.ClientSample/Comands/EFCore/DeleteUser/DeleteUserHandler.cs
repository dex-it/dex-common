using Dex.Audit.ClientSample.Context;
using MediatR;

namespace Dex.Audit.ClientSample.Comands.EFCore.DeleteUser;

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