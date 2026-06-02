using Microsoft.AspNetCore.Mvc;

namespace Dex.RfcExceptionsHandler.Rfc;

/// <summary>
/// Provides helper methods for converting exceptions to <see cref="ProblemDetails"/>.
/// </summary>
public static class RfcHelper
{
    /// <summary>
    /// Converts the specified exception to a <see cref="ProblemDetails"/> instance.
    /// </summary>
    /// <typeparam name="T">
    /// The exception type.
    /// </typeparam>
    /// <param name="exception">
    /// The exception to convert.
    /// </param>
    /// <returns>
    /// A <see cref="ProblemDetails"/> instance.
    /// </returns>
    public static ProblemDetails ToProblemDetails<T>(this T exception) where T : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

        var extensions = new Dictionary<string, object?>(RfcExtensions.DefaultExtensionsCapacity)
        {
            [RfcExtensions.ExceptionType] = exception.GetType().Name.Replace("`1", string.Empty, StringComparison.Ordinal)
        };

        if (exception.Data.Count > 0)
            extensions[RfcExtensions.ExceptionData] = exception.Data;

        // generic вариант, для ошибок, не реализующих IRfcException
        if (exception is not IRfcException rfcException)
            return new ProblemDetails
            {
                Title = "Unexpected error occurred",
                Detail = exception.Message,
                Extensions = extensions
            };

        // custom rfc extensions
        if (rfcException.RfcExtensions.Count > 0)
            foreach (var rfcExtension in rfcException.RfcExtensions)
                extensions[rfcExtension.Key] = rfcExtension.Value;

        var problemDetails = new ProblemDetails
        {
            Type = rfcException.RfcType,
            Title = rfcException.Title,
            Status = rfcException.StatusCode,
            Detail = rfcException.Detail,
            Extensions = extensions
        };

        return problemDetails;
    }
}