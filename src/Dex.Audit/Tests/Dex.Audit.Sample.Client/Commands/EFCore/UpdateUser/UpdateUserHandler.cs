using Dex.Audit.ClientSample.Context;
using MediatR;

namespace Dex.Audit.ClientSample.Commands.EFCore.UpdateUser;

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