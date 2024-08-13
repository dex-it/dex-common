using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Client.Abstractions.Options;
using Dex.Audit.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Dex.Audit.Client.Consumers;

/// <summary>
/// Обработчик аудиторских событий, полученных через шину сообщений.
/// </summary>
/// <param name="auditCacheRepository"><see cref="IAuditCacheRepository"/>.</param>
public class AuditSettingsConsumer(
    IOptions<AuditCacheOptions> options,
    IAuditCacheRepository auditCacheRepository) : IConsumer<UpdatedAuditSettingsMessage>
{
    /// <summary>
    /// Метод для обработки аудиторских событий, полученных через шину сообщений.
    /// </summary>
    /// <param name="context">Контекст сообщения, содержащий аудиторское событие для обработки.</param>
    public async Task Consume(ConsumeContext<UpdatedAuditSettingsMessage> context)
    {
        var settings = context.Message.UpdatedAuditSettingsDtos.Select(setting => new AuditSettings
        {
            Id = setting.Id,
            EventType = setting.EventType,
            SeverityLevel = setting.SeverityLevel
        });

        await auditCacheRepository.AddRangeAsync(settings, options.Value.RefreshInterval, context.CancellationToken);
    }
}