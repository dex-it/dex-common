using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

/// <inheritdoc/>>
public class DefaultRfcExceptionHandleConfig : IRfcExceptionHandleConfig
{
    /// <summary>
    /// Client Closed Request
    /// </summary>
    private const int Status499ClientClosedRequest = 499;

    /// <inheritdoc/>>
    public JsonSerializerOptions JsonSerializerOptions => JsonSerializerOptions.Default;

    /// <inheritdoc/>>
    public virtual Exception Map(Exception exception) => exception;

    /// <inheritdoc/>>
    public virtual int ResolveHttpStatusCode(Exception exception) => exception switch
    {
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,

        ArgumentException or ValidationException => StatusCodes.Status400BadRequest,

        TimeoutException => StatusCodes.Status408RequestTimeout,
        OperationCanceledException => Status499ClientClosedRequest,

        RpcException x => x.StatusCode switch
        {
            StatusCode.Unavailable => StatusCodes.Status408RequestTimeout,
            StatusCode.Cancelled => Status499ClientClosedRequest,
            StatusCode.NotFound => StatusCodes.Status404NotFound,
            StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
            StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            StatusCode.AlreadyExists => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        },

        _ => StatusCodes.Status500InternalServerError
    };
}