using AuditGrpcServer;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Server.Grpc.Extensions;

/// <summary>
/// Расширение для маппинга.
/// </summary>
internal static class MappingExtensions
{
    /// <summary>
    /// Замапить сущность <see cref="AuditSettings"/> в grpc сообщение <see cref="AuditSettingsMessage"/>.
    /// </summary>
    /// <param name="auditSettings"><see cref="AuditSettings"/></param>
    /// <returns><see cref="AuditSettingsMessage"/><see cref="AuditSettings"/></returns>
    internal static AuditSettingsMessage MapToAuditSettingsMessage(this AuditSettings auditSettings)
    {
        return new AuditSettingsMessage
        {
            Id = auditSettings.Id.ToString(),
            SeverityLevel = auditSettings.SeverityLevel.ToString(),
            EventType = auditSettings.EventType
        };
    }
}