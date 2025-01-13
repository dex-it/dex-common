namespace Dex.Audit.Client.Abstractions.Messages;

/// <summary>
/// Basic information about the audit event.
/// </summary>
public sealed class AuditEventBaseInfo
{
    /// <summary>
    /// Event type.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// The object of the audit message.
    /// </summary>
    public string? EventObject { get; }

    /// <summary>
    /// The text of the event message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Result (success/failure) of the action.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Initializes a new instance of the class <see cref="AuditEventBaseInfo"/>.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventObject">The object of the audit message.</param>
    /// <param name="message">The text of the event message.</param>
    /// <param name="isSuccess">Result (success/failure) of the action.</param>
    public AuditEventBaseInfo(string eventType, string? eventObject, string? message, bool isSuccess)
    {
        EventType = eventType;
        EventObject = eventObject;
        Message = message;
        IsSuccess = isSuccess;
    }
}
