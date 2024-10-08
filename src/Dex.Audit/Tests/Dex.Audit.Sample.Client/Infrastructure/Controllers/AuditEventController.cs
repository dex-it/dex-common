using Dex.Audit.ClientSample.Application.Commands.AuditEvents;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.ClientSample.Infrastructure.Controllers;

public class AuditEventController(IMediator mediator) : BaseController
{
    [HttpPost]
    public Task Add(AddAuditEventCommand command)
    {
        return mediator.Send(command);
    }
}