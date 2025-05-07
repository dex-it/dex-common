using Dex.Audit.Sample.Client.Application.Commands.Logs;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.Sample.Client.Infrastructure.Controllers;

public class LogsController(ISender mediator) : BaseController
{
    [HttpPost]
    public Task<AddAuditableLogResponse> Add(AddAuditableLogCommand command)
    {
        return mediator.Send(command);
    }
}