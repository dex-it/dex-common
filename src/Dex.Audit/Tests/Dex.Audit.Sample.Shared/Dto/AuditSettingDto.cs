using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Sample.Shared.Dto;

public record AuditSettingDto(Guid Id, string EventType, AuditEventSeverityLevel SeverityLevel);