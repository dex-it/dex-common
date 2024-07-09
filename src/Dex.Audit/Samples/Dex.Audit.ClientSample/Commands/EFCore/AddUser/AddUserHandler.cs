using Dex.Audit.ClientSample.Context;
using Dex.Audit.ClientSample.Entities;
using MediatR;

namespace Dex.Audit.ClientSample.Commands.EFCore.AddUser;

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