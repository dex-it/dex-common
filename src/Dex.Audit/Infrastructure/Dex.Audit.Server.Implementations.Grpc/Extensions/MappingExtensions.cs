using AuditGrpcServer;
using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Server.Implementations.Grpc.Extensions;

/// <summary>
/// Mapping extensions.
/// </summary>
internal static class MappingExtensions
{
    /// <summary>
    /// Map <see cref="AuditSettings"/> to <see cref="AuditSettingsMessage"/>.
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