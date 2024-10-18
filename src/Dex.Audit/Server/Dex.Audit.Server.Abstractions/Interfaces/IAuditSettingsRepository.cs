using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// The repository of the permanent settings repository.
/// </summary>
public interface IAuditSettingsRepository
{
    /// <summary>
    /// Add or update settings.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="severityLevel">The level of severity of the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddOrUpdateSettings(
        string eventType,
        AuditEventSeverityLevel severityLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete settings.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task DeleteSettings(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all settings.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
}