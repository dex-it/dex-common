using Dex.Audit.ClientSample.Application.Commands.Logs;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.ClientSample.Infrastructure.Controllers;

public class LogsController(ISender mediator) : BaseController
{
    [HttpPost]
    public Task<AddAuditableLogResponse> Add(AddAuditableLogCommand command)
    {
        return mediator.Send(command);
    }
}