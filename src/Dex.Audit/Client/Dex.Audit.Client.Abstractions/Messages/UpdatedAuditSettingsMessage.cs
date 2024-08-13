using Dex.Audit.Client.Abstractions.Dtos;

namespace Dex.Audit.Client.Abstractions.Messages;

public class UpdatedAuditSettingsMessage
{
    public AuditSettingsDto[] UpdatedAuditSettingsDtos { get; set; }
}