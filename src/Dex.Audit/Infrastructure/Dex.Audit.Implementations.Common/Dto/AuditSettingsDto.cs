namespace Dex.Audit.Implementations.Common.Dto;

public sealed record AuditSettingsDto(IEnumerable<AuditSettingDto> AuditSettingDtos);