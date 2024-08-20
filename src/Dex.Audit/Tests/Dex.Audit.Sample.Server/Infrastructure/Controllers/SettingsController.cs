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
    public async Task AddOrUpdate(IAuditServerSettingsService settingsServer, string eventType, AuditEventSeverityLevel severityLevel)
    {
        await settingsServer.AddOrUpdateSettingsAsync(eventType, severityLevel);
    }

    [HttpDelete]
    public async Task Delete(IAuditServerSettingsService settingsServer, string eventType)
    {
        await settingsServer.DeleteSettingsAsync(eventType);
    }
}