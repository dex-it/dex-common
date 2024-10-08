using Dex.Audit.ClientSample.Application.Commands.Users;
using Dex.Audit.ClientSample.Application.Queries.Users;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.ClientSample.Infrastructure.Controllers;

public class UsersController(IMediator mediator) : BaseController
{
    [HttpGet]
    public Task<GetUserQueryResponse> Get(GetUserQuery command)
    {
        return mediator.Send(command);
    }

    [HttpPost]
    public Task<AddUserResponse> Add(AddUserCommand command)
    {
        return mediator.Send(command);
    }

    [HttpPut]
    public Task<UpdateUserResponse> Update(UpdateUserCommand command)
    {
        return mediator.Send(command);
    }

    [HttpDelete]
    public Task<DeleteUserResponse> Delete(DeleteUserCommand command)
    {
        return mediator.Send(command);
    }
}