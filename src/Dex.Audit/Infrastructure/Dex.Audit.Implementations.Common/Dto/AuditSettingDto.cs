using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Implementations.Common.Dto;

/// <summary>
/// Audit Setting Dto for publishing updated settings to clients. 
/// </summary>
/// <param name="Id">Identifier of audit settings.</param>
/// <param name="EventType">Event type.</param>
/// <param name="SeverityLevel">Severity level.</param>
public sealed record AuditSettingDto(Guid Id, string EventType, AuditEventSeverityLevel SeverityLevel);