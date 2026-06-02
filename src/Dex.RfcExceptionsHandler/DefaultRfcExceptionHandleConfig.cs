using System.ComponentModel.DataAnnotations;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

public class DefaultRfcExceptionHandleConfig : IRfcExceptionHandleConfig
{
    /// <summary>
    /// Client Closed Request
    /// </summary>
    private const int Status499ClientClosedRequest = 499;

    public virtual Exception Map(Exception exception) => exception;

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
            _ => StatusCodes.Status500InternalServerError
        },

        _ => StatusCodes.Status500InternalServerError
    };
}