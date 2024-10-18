using System.Text.Json.Serialization;
using MediatR;

namespace Dex.Audit.MediatR.Requests;

/// <summary>
/// Interface for audit requests that return a result of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the result returned by the request.</typeparam>
public abstract class AuditRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// The type of the audit event.
    /// </summary>
    [JsonIgnore]
    public abstract string EventType { get; }

    /// <summary>
    /// The object associated with the audit event.
    /// </summary>
    [JsonIgnore]
    public abstract string EventObject { get; }

    /// <summary>
    /// The message describing the audit event.
    /// </summary>
    [JsonIgnore]
    public abstract string Message { get; }
}