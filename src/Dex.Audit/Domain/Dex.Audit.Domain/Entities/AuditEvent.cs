using Dex.Audit.Domain.ValueObjects;

namespace Dex.Audit.Domain.Entities;

/// <summary>
/// Audit event.
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Identifier of the security event entry within a single system log.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Event type.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Information about the event source.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// Information about the event destination object.
    /// </summary>
    public required Destination Destination { get; init; }

    /// <summary>
    /// Event object.
    /// </summary>
    public required string EventObject { get; init; }

    /// <summary>
    /// Message text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Result (success/failure).
    /// </summary>
    public bool IsSuccess { get; init; }
}