using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

public class DefaultRfcExceptionHandleConfig : IRfcExceptionHandleConfig
{
    public JsonSerializerOptions JsonSerializerOptions => JsonSerializerOptions.Default;

    public virtual Exception Map(Exception exception) => exception;

    public virtual int ResolveHttpStatusCode(Exception exception) => exception switch
    {
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,

        ArgumentException or ValidationException => StatusCodes.Status400BadRequest,

        TimeoutException => StatusCodes.Status408RequestTimeout,
        OperationCanceledException => StatusCodes.Status499ClientClosedRequest,

        RpcException x => x.StatusCode switch
        {
            StatusCode.Unavailable => StatusCodes.Status408RequestTimeout,
            StatusCode.Cancelled => StatusCodes.Status499ClientClosedRequest,
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