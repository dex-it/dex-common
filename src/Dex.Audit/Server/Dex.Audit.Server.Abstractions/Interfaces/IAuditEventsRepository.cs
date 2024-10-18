using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Server.Abstractions.Interfaces;

/// <summary>
/// The repository of the permanent event repository.
/// </summary>
public interface IAuditEventsRepository
{
    /// <summary>
    /// Add event.
    /// </summary>
    /// <param name="auditEvents">Event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task AddAuditEventsRangeAsync(
        IEnumerable<AuditEvent> auditEvents,
        CancellationToken cancellationToken = default);
}