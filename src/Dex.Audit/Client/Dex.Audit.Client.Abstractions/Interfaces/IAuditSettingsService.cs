using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

public interface IAuditSettingsService
{
    Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default);
}