using AuditGrpcServer;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Grpc.Extensions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Server.Grpc.Services;

public class GrpcAuditServerSettingsService(
    IServiceProvider serviceProvider,
    ILogger<GrpcAuditServerSettingsService> logger)
    : AuditSettingsService.AuditSettingsServiceBase
{
    private static readonly SemaphoreSlim ClientsSemaphore = new(1, 1);
    private readonly List<IServerStreamWriter<AuditSettingsMessages>> _clients = new();

    internal void NotifyClients()
    {
        if (_clients.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ClientsSemaphore
                    .WaitAsync()
                    .ConfigureAwait(false);

                var messages = await GetMessages()
                    .ConfigureAwait(false);

                foreach (var client in _clients)
                {
                    await NotifyClientAsync(client, messages)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "An error occurred while notifying clients: {Message}.",
                    exception.Message);
            }
            finally
            {
                ClientsSemaphore.Release();
            }
        });
    }

    public override Task<AuditSettingsMessages> GetSettings(
        Empty request,
        ServerCallContext context)
    {
        return GetMessages(context.CancellationToken);
    }

    public override async Task GetSettingsStream(
        Empty request,
        IServerStreamWriter<AuditSettingsMessages> responseStream,
        ServerCallContext context)
    {
        try
        {
            await ClientsSemaphore
                .WaitAsync()
                .ConfigureAwait(false);
            _clients.Add(responseStream);
        }
        finally
        {
            ClientsSemaphore.Release();
        }

        context.CancellationToken.WaitHandle.WaitOne();

        try
        {
            await ClientsSemaphore
                .WaitAsync()
                .ConfigureAwait(false);
            _clients.Remove(responseStream);
        }
        finally
        {
            ClientsSemaphore.Release();
        }
    }

    private async Task<AuditSettingsMessages> GetMessages(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var settings = await scope.ServiceProvider
            .GetRequiredService<IAuditSettingsRepository>()
            .GetAllSettingsAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = new AuditSettingsMessages();

        messages.Messages
            .AddRange(settings.Select(auditSettings => auditSettings.MapToAuditSettingsMessage()));

        return messages;
    }

    private async Task NotifyClientAsync(IServerStreamWriter<AuditSettingsMessages> client, AuditSettingsMessages messages)
    {
        try
        {
            await client
                .WriteAsync(messages)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "An error occurred while notifying client: {Message}.",
                exception.Message);
        }
    }
}