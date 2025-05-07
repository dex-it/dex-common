using Dex.Audit.Sample.Client.Application.Commands.Users;
using Dex.Audit.Sample.Client.Application.Queries.Users;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.Sample.Client.Infrastructure.Controllers;

public class UsersController(ISender mediator) : BaseController
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