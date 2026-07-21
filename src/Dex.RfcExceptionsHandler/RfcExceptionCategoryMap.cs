using Dex.RfcExceptions;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

/// <summary>Маппинг категории проблемы в HTTP-статус и RFC 9457 type.</summary>
public static class RfcExceptionCategoryMap
{
    public static (int Status, string Type) Resolve(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation         => (StatusCodes.Status400BadRequest, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ValidationError),
        ErrorCategory.BadRequest         => (StatusCodes.Status400BadRequest, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.BadRequest),
        ErrorCategory.Unauthorized       => (StatusCodes.Status401Unauthorized, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Unauthorized),
        ErrorCategory.Forbidden          => (StatusCodes.Status403Forbidden, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Forbidden),
        ErrorCategory.NotFound           => (StatusCodes.Status404NotFound, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotFound),
        ErrorCategory.Conflict           => (StatusCodes.Status409Conflict, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Conflict),
        ErrorCategory.AlreadyExists      => (StatusCodes.Status409Conflict, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.AlreadyExist),
        ErrorCategory.PreconditionFailed => (StatusCodes.Status412PreconditionFailed, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PreconditionFailed),
        ErrorCategory.PaymentRequired    => (StatusCodes.Status402PaymentRequired, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PaymentError),
        ErrorCategory.TooManyRequests    => (StatusCodes.Status429TooManyRequests, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.TooManyRequests),
        ErrorCategory.Timeout            => (StatusCodes.Status408RequestTimeout, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.RequestTimeout),
        ErrorCategory.IntegrationError   => (StatusCodes.Status412PreconditionFailed, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.IntegrationError),
        ErrorCategory.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ServiceUnavailable),
        _                                => (StatusCodes.Status500InternalServerError, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.InternalServerError)
    };
}