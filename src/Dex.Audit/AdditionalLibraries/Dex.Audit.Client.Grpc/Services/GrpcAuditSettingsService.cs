using AuditGrpcServer;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Grpc.Extensions;
using Dex.Audit.Domain.Entities;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Grpc.Services;

internal class GrpcAuditSettingsService(
    ILogger<GrpcAuditSettingsService> logger,
    IAuditCacheRepository cacheRepository,
    AuditSettingsService.AuditSettingsServiceClient grpcClient)
    : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await cacheRepository
                .GetAsync(eventType, cancellationToken)
                .ConfigureAwait(false);

            if (setting != null)
            {
                return setting;
            }

            var settings = await grpcClient
                .GetSettingsAsync(new Empty(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await cacheRepository
                .AddRangeAsync(settings.Messages.Select(message => message.MapToAuditSettings()), cancellationToken)
                .ConfigureAwait(false);

            return await cacheRepository
                .GetAsync(eventType, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            logger.LogError(exception, exception.Message);
        }

        return null;
    }
}