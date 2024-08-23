using AuditGrpcServer;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Grpc.Extensions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Grpc.Services;

public class GrpcAuditServerSettingsService(IServiceProvider serviceProvider, ILogger<GrpcAuditServerSettingsService> logger) : AuditSettingsService.AuditSettingsServiceBase
{
    private readonly List<IServerStreamWriter<AuditSettingsMessages>> _clients = new();
    
    internal void NotifyClients()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var messages = await GetMessagesAsync().ConfigureAwait(false);

                foreach (var client in _clients)
                {
                    await client.WriteAsync(messages).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error due notifying clients {Message}.", exception.Message);
            }
        });
    }

    public override async Task<AuditSettingsMessages> GetSettings(Empty request, ServerCallContext context)
    {
        return await GetMessagesAsync(context.CancellationToken).ConfigureAwait(false);
    }

    public override async Task GetSettingsStream(Empty request, IServerStreamWriter<AuditSettingsMessages> responseStream, ServerCallContext context)
    {
        _clients.Add(responseStream);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000).ConfigureAwait(false);
        }

        _clients.Remove(responseStream);
    }

    private async Task<AuditSettingsMessages> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var settings = await scope.ServiceProvider
            .GetRequiredService<IAuditPersistentRepository>()
            .GetAllSettingsAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = new AuditSettingsMessages();

        messages.Messages
            .AddRange(settings.Select(auditSettings => auditSettings.MapToAuditSettings()));

        return messages;
    }
}