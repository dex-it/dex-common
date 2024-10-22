using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Implementations.Common.Dto;

public sealed record AuditSettingDto(Guid Id, string EventType, AuditEventSeverityLevel SeverityLevel);