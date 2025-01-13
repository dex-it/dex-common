using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// The interface of the audit settings service.
/// </summary>
public interface IAuditSettingsService
{
    /// <summary>
    /// Get or get and update settings.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Audit settings.</returns>
    Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}