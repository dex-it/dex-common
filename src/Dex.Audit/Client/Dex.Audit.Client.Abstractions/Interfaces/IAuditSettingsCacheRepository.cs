using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// The repository of the cached storage.
/// </summary>
public interface IAuditSettingsCacheRepository
{
    /// <summary>
    /// Get settings from the cache by event type.
    /// </summary>
    /// <param name="eventType">Event Type.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task<AuditSettings?> GetAsync(
        string eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get settings from the cache by event types.
    /// </summary>
    /// <param name="eventTypes">Event types.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task<IDictionary<string, AuditSettings?>> GetDictionaryAsync(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add settings to the cache.
    /// </summary>
    /// <param name="settings">Audit setting.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task AddRangeAsync(
        IEnumerable<AuditSettings> settings,
        CancellationToken cancellationToken = default);
}