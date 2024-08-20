using Dex.Audit.ClientSample.Application.Commands.Users;
using Dex.Audit.ClientSample.Application.Queries.Users;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.ClientSample.Infrastructure.Controllers;

public class UsersController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<GetUserQueryResponse> Get(GetUserQuery command)
    {
        return await mediator.Send(command);
    }

    [HttpPost]
    public async Task<AddUserResponse> Add(AddUserCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpPut]
    public async Task<UpdateUserResponse> Update(UpdateUserCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpDelete]
    public async Task<DeleteUserResponse> Delete(DeleteUserCommand command)
    {
        return await mediator.Send(command);
    }
}