namespace Dex.Audit.Implementations.Common.Dto;

/// <summary>
/// Audit Settings Dto for publishing updated settings to clients.
/// </summary>
/// <param name="AuditSettingDtos">Collection of audit setting.</param>
public sealed record AuditSettingsDto(IEnumerable<AuditSettingDto> AuditSettingDtos);