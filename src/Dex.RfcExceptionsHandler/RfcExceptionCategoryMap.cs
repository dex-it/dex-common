using Dex.RfcExceptions;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

/// <summary>Маппинг категории проблемы в HTTP-статус и RFC 9457 type.</summary>
public static class RfcExceptionCategoryMap
{
    public static (int Status, string Type) Resolve(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation         => (StatusCodes.Status400BadRequest, RfcTypes.ValidationError),
        ErrorCategory.BadRequest         => (StatusCodes.Status400BadRequest, RfcTypes.BadRequest),
        ErrorCategory.Unauthorized       => (StatusCodes.Status401Unauthorized, RfcTypes.Unauthorized),
        ErrorCategory.Forbidden          => (StatusCodes.Status403Forbidden, RfcTypes.Forbidden),
        ErrorCategory.NotFound           => (StatusCodes.Status404NotFound, RfcTypes.NotFound),
        ErrorCategory.Conflict           => (StatusCodes.Status409Conflict, RfcTypes.Conflict),
        ErrorCategory.AlreadyExists      => (StatusCodes.Status409Conflict, RfcTypes.AlreadyExist),
        ErrorCategory.PreconditionFailed => (StatusCodes.Status412PreconditionFailed, RfcTypes.PreconditionFailed),
        ErrorCategory.PaymentRequired    => (StatusCodes.Status402PaymentRequired, RfcTypes.PaymentError),
        ErrorCategory.TooManyRequests    => (StatusCodes.Status429TooManyRequests, RfcTypes.TooManyRequests),
        ErrorCategory.Timeout            => (StatusCodes.Status408RequestTimeout, RfcTypes.RequestTimeout),
        ErrorCategory.IntegrationError   => (StatusCodes.Status412PreconditionFailed, RfcTypes.IntegrationError),
        ErrorCategory.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, RfcTypes.ServiceUnavailable),
        _                                => (StatusCodes.Status500InternalServerError, RfcTypes.InternalServerError)
    };
}