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
        _ = Task.Run(async () =>
        {
            try
            {
                await ClientsSemaphore
                    .WaitAsync()
                    .ConfigureAwait(false);

                if (_clients.Count == 0)
                {
                    return;
                }

                var messages = await GetMessagesAsync()
                    .ConfigureAwait(false);

                foreach (var client in _clients)
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

    public override async Task<AuditSettingsMessages> GetSettings(
        Empty request,
        ServerCallContext context)
    {
        return await GetMessagesAsync(context.CancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task GetSettingsStream(
        Empty request,
        IServerStreamWriter<AuditSettingsMessages> responseStream,
        ServerCallContext context)
    {
        await ClientsSemaphore
            .WaitAsync()
            .ConfigureAwait(false);
        _clients.Add(responseStream);
        ClientsSemaphore.Release();

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await Task
                .Delay(1000)
                .ConfigureAwait(false);
        }

        await ClientsSemaphore
            .WaitAsync()
            .ConfigureAwait(false);
        _clients.Remove(responseStream);
        ClientsSemaphore.Release();
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
            .AddRange(settings.Select(auditSettings => auditSettings.MapToAuditSettingsMessage()));

        return messages;
    }
}