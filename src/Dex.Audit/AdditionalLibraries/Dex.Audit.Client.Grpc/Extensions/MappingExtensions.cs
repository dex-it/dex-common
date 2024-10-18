using AuditGrpcServer;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Client.Grpc.Extensions;

/// <summary>
/// MappingExtensions.
/// </summary>
internal static class MappingExtensions
{
    /// <summary>
    /// Map <see cref="AuditSettingsMessage"/> to <see cref="AuditSettings"/>.
    /// </summary>
    /// <param name="auditSettingsMessage"><see cref="AuditSettingsMessage"/></param>
    /// <returns><see cref="AuditSettings"/></returns>
    internal static AuditSettings MapToAuditSettings(this AuditSettingsMessage auditSettingsMessage)
    {
        return new AuditSettings
        {
            Id = new Guid(auditSettingsMessage.Id),
            EventType = auditSettingsMessage.EventType,
            SeverityLevel = Enum.Parse<AuditEventSeverityLevel>(auditSettingsMessage.SeverityLevel)
        };
    }
}