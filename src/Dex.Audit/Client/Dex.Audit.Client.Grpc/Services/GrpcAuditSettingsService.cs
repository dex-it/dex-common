using AuditGrpcServer;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Grpc.Services;

public class GrpcAuditSettingsService(
    ILogger<GrpcAuditSettingsService> logger,
    IAuditCacheRepository cacheRepository,
    AuditSettingsService.AuditSettingsServiceClient grpcClient) : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await cacheRepository.GetAsync(eventType, cancellationToken).ConfigureAwait(false);

            if (setting != null)
            {
                return setting;
            }

            var call = grpcClient.GetSettingsAsync(new Empty());
            var settings = await call;

            await cacheRepository.AddRangeAsync(settings.Messages.Select(message => new AuditSettings
                {
                    Id = new Guid(message.Id),
                    EventType = message.EventType,
                    SeverityLevel = Enum.Parse<AuditEventSeverityLevel>(message.SeverityLevel)
                }),
                cancellationToken);

            return await cacheRepository.GetAsync(eventType, cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            logger.LogError(exception, exception.Message);
        }

        return null;
    }
}