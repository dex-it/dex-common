using Dex.Audit.Sample.Client.Application.Commands.AuditEvents;
using Dex.Audit.Sample.Shared.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dex.Audit.Sample.Client.Infrastructure.Controllers;

public class AuditEventController(ISender mediator) : BaseController
{
    [HttpPost]
    public Task Add(AddAuditEventCommand command)
    {
        return mediator.Send(command);
    }
}