using AuditClient;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Grpc.Workers;

public class GrpcAuditBackgroundWorker(ILogger<GrpcAuditBackgroundWorker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<AuditSettingsService.AuditSettingsServiceClient>();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IAuditCacheRepository>();

        while (stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var call = client.GetSettingsStream(new Empty(), cancellationToken: stoppingToken);

                await foreach (var cat in call.ResponseStream.ReadAllAsync(cancellationToken: stoppingToken).ConfigureAwait(false))
                {
                    await cacheRepository.AddRangeAsync(cat.Messages.Select(message => new AuditSettings()
                        {
                            Id = new Guid(message.Id),
                            EventType = message.EventType,
                            SeverityLevel = Enum.Parse<AuditEventSeverityLevel>(message.SeverityLevel)
                        }),
                        stoppingToken)
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