using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptions;

/// <summary>
/// Represents an application exception that can be converted to
/// an RFC 7807 / HTTP API error response.
/// </summary>
/// <remarks>
/// Use this contract for domain and application exceptions that should be
/// exposed to API clients in a stable and predictable format.
/// <para>
/// The values returned by this interface are used to populate
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
/// </para>
/// </remarks>
public interface IRfcException
{
    protected internal static readonly ImmutableDictionary<string, string> NoExtensions = ImmutableDictionary.Create<string, string>();

    /// <summary>
    /// Gets the HTTP status code that should be returned to the client.
    /// </summary>
    /// <value>
    /// A valid HTTP status code, for example
    /// <see cref="Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest"/>,
    /// <see cref="StatusCodes.Status404NotFound"/>,
    /// <see cref="StatusCodes.Status409Conflict"/>.
    /// </value>
    int StatusCode { get; }

    /// <summary>
    /// Gets the stable problem type identifier.
    /// </summary>
    /// <value>
    /// A stable URI or URI-like string that identifies the category of the problem,
    /// for example <c>/problems/not-found</c> or <c>/problems/validation-error</c>.
    /// </value>
    /// <remarks>
    /// This value should describe the problem category, not the .NET exception type.
    /// </remarks>
    string RfcType { get; }

    /// <summary>
    /// Gets the short, human-readable summary of the problem type.
    /// </summary>
    /// <value>
    /// A stable title such as <c>Resource not found</c>,
    /// <c>Validation failed</c>, or <c>Conflict</c>.
    /// </value>
    string Title { get; }

    /// <summary>
    /// Gets the human-readable explanation specific to the current occurrence.
    /// </summary>
    /// <value>
    /// Additional details for the current error instance.
    /// This value may be <see langword="null"/> when no extra details should be exposed.
    /// </value>
    /// <remarks>
    /// Do not put stack traces, secrets, connection strings, or other internal diagnostics here.
    /// </remarks>
    string? Detail { get; }

    IDictionary<string, string> RfcExtensions => NoExtensions;
}