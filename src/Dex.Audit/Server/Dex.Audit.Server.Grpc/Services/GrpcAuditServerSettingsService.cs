using AuditGrpcServer;
using Dex.Audit.Server.Abstractions.Interfaces;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Server.Grpc.Services;

public class GrpcAuditServerSettingsService(IServiceProvider serviceProvider) : AuditSettingsService.AuditSettingsServiceBase
{
    private readonly List<IServerStreamWriter<AuditSettingsMessages>> _clients = new();
    
    internal async void NotifyClients()
    {
        using var scope = serviceProvider.CreateScope();
        var settings = await scope.ServiceProvider.GetRequiredService<IAuditPersistentRepository>().GetAllSettingsAsync();
        var messages = new AuditSettingsMessages();
        messages.Messages
            .AddRange(settings.Select(auditSettings => new AuditSettingsMessage
            {
                Id = auditSettings.Id.ToString(),
                SeverityLevel = auditSettings.SeverityLevel.ToString(),
                EventType = auditSettings.EventType
            }));
        foreach (var client in _clients)
        {
            await client.WriteAsync(messages);
        }
    }

    public override async Task<AuditSettingsMessages> GetSettings(Empty request, ServerCallContext context)
    {
        using var scope = serviceProvider.CreateScope();
        var settings = await scope.ServiceProvider.GetRequiredService<IAuditPersistentRepository>().GetAllSettingsAsync();
        var messages = new AuditSettingsMessages();
        messages.Messages
            .AddRange(settings.Select(auditSettings => new AuditSettingsMessage
            {
                Id = auditSettings.Id.ToString(),
                SeverityLevel = auditSettings.SeverityLevel.ToString(),
                EventType = auditSettings.EventType
            }));
        return messages;
    }

    public override async Task GetSettingsStream(Empty request, IServerStreamWriter<AuditSettingsMessages> responseStream, ServerCallContext context)
    {
        _clients.Add(responseStream);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }

        _clients.Remove(responseStream);
    }
}