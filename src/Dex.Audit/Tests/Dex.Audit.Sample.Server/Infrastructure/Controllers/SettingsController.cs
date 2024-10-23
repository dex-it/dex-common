using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Shared.Api;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ServerSample.Infrastructure.Controllers;

public class SettingsController : BaseController
{
    [HttpGet]
    public async Task<IEnumerable<AuditSettings>> Get([FromServices] AuditServerDbContext context)
    {
        return await context.AuditSettings.ToListAsync();
    }

    [HttpPut]
    public Task AddOrUpdate(IAuditServerSettingsService settingsServer, string eventType, AuditEventSeverityLevel severityLevel)
    {
        return settingsServer.AddOrUpdateSettingsAsync(eventType, severityLevel);
    }

    [HttpDelete]
    public Task Delete(IAuditServerSettingsService settingsServer, string eventType)
    {
        return settingsServer.DeleteSettingsAsync(eventType);
    }
}