using Dex.Audit.Domain.Entities;
using Dex.Audit.Sample.Server.Infrastructure.Context;
using Dex.Audit.Sample.Shared.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.Sample.Server.Infrastructure.Controllers;

public class EventsController : BaseController
{
    [HttpGet]
    public async Task<IEnumerable<AuditEvent>> Get([FromServices] AuditServerDbContext context)
    {
        return await context.AuditEvents.ToListAsync();
    }
}