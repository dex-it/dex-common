using AuditGrpcServer;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Grpc.Extensions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Grpc.Workers;

internal class GrpcAuditBackgroundWorker(ILogger<GrpcAuditBackgroundWorker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<AuditSettingsService.AuditSettingsServiceClient>();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IAuditCacheRepository>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var call = client
                    .GetSettingsStream(new Empty(), cancellationToken: stoppingToken);

                await foreach (var cat in call.ResponseStream
                                   .ReadAllAsync(cancellationToken: stoppingToken)
                                   .ConfigureAwait(false))
                {
                    await cacheRepository
                        .AddRangeAsync(cat.Messages.Select(message => message.MapToAuditSettings()), stoppingToken)
                        .ConfigureAwait(false);
                }
            }
            catch (RpcException exception)
            {
                logger.LogError(exception, exception.Message);
            }
        }
    }
}