using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// Settings management service.
/// </summary>
public interface IAuditServerSettingsService
{
    /// <summary>
    /// Add or update settings.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="severityLevel">The level of severity of the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddOrUpdateSettingsAsync(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete settings.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task DeleteSettingsAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}