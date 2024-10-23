using Dex.Audit.Client.Abstractions.Messages;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Interface for managing (configuring and sending) audit events.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Processes the audit event based on the basic information about the event.
    /// </summary>
    /// <param name="eventBaseInfo">Basic information about the audit event.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    public Task WriteAsync(
        AuditEventBaseInfo eventBaseInfo,
        CancellationToken cancellationToken = default);
}
