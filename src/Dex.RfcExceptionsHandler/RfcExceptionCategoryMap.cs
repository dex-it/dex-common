using Dex.RfcAbstractions;
using Microsoft.AspNetCore.Http;

namespace Dex.RfcExceptionsHandler;

/// <summary>
/// Маппинг категории проблемы в HTTP-статус и код ошибки (без префикса /problems/).
/// Полный RFC 9457 type собирается в middleware: RfcTypeConstants.ProblemTypePrefix + Code.
/// </summary>
public static class RfcExceptionCategoryMap
{
    public static (int Status, string Code) Resolve(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => (StatusCodes.Status400BadRequest, RfcErrorCodes.ValidationError),
        ErrorCategory.BadRequest => (StatusCodes.Status400BadRequest, RfcErrorCodes.BadRequest),
        ErrorCategory.UserError => (StatusCodes.Status400BadRequest, RfcErrorCodes.UserError),
        ErrorCategory.Unauthorized => (StatusCodes.Status401Unauthorized, RfcErrorCodes.Unauthorized),
        ErrorCategory.Forbidden => (StatusCodes.Status403Forbidden, RfcErrorCodes.Forbidden),
        ErrorCategory.NotFound => (StatusCodes.Status404NotFound, RfcErrorCodes.NotFound),
        ErrorCategory.Conflict => (StatusCodes.Status409Conflict, RfcErrorCodes.Conflict),
        ErrorCategory.AlreadyExists => (StatusCodes.Status409Conflict, RfcErrorCodes.AlreadyExist),
        ErrorCategory.PreconditionFailed => (StatusCodes.Status412PreconditionFailed, RfcErrorCodes.PreconditionFailed),
        ErrorCategory.PaymentRequired => (StatusCodes.Status402PaymentRequired, RfcErrorCodes.PaymentError),
        ErrorCategory.TooManyRequests => (StatusCodes.Status429TooManyRequests, RfcErrorCodes.TooManyRequests),
        ErrorCategory.Timeout => (StatusCodes.Status408RequestTimeout, RfcErrorCodes.Timeout),
        ErrorCategory.IntegrationError => (StatusCodes.Status503ServiceUnavailable, RfcErrorCodes.IntegrationError),
        ErrorCategory.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, RfcErrorCodes.ServiceUnavailable),
        _ => (StatusCodes.Status500InternalServerError, RfcErrorCodes.InternalServerError)
    };
}