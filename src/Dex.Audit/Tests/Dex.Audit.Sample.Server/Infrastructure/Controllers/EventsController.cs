using Dex.Audit.Domain.Entities;
using Dex.Audit.Sample.Shared.Api;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Infrastructure.Controllers;

public class EventsController : BaseController
{
    [HttpGet]
    public async Task<IEnumerable<AuditEvent>> Get([FromServices] AuditServerDbContext context)
    {
        return await context.AuditEvents.ToListAsync();
    }
}