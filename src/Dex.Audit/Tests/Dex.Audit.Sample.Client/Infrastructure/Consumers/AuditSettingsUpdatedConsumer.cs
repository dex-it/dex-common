using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Sample.Shared.Dto;
using MassTransit;

namespace Dex.Audit.ClientSample.Infrastructure.Consumers;

public class AuditSettingsUpdatedConsumer(IAuditCacheRepository cacheRepository) : IConsumer<AuditSettingsDto>
{
    public async Task Consume(ConsumeContext<AuditSettingsDto> context)
    {
        var settings = context.Message.AuditSettingDtos
            .Select(dto => new AuditSettings
            {
                Id = dto.Id,
                EventType = dto.EventType,
                SeverityLevel = dto.SeverityLevel
            });

        await cacheRepository.AddRangeAsync(settings, context.CancellationToken);
    }
}