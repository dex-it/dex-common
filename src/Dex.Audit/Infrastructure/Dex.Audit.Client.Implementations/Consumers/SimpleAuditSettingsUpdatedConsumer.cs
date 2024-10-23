using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Implementations.Common.Dto;
using MassTransit;

namespace Dex.Audit.Client.Grpc.Consumers;

/// <summary>
/// Simple implementation of <see cref="IConsumer{T}"/> for <see cref="AuditSettingsDto"/>.
/// </summary>
/// <param name="settingsCacheRepository"><see cref="IAuditSettingsCacheRepository"/></param>
public class SimpleAuditSettingsUpdatedConsumer(IAuditSettingsCacheRepository settingsCacheRepository) : IConsumer<AuditSettingsDto>
{
    public Task Consume(ConsumeContext<AuditSettingsDto> context)
    {
        var settings = context.Message.AuditSettingDtos
            .Select(dto => new AuditSettings
            {
                Id = dto.Id,
                EventType = dto.EventType,
                SeverityLevel = dto.SeverityLevel
            });

        return settingsCacheRepository.AddRangeAsync(settings, context.CancellationToken);
    }
}