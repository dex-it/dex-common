using Dex.Audit.ClientSample.Application.Commands.Logs;
using Dex.Audit.ClientSample.Infrastructure.Controllers.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.ClientSample.Infrastructure.Controllers;

public class LogsController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<AddAuditableLogResponse> Add(AddAuditableLogCommand command)
    {
        return await mediator.Send(command);
    }
}