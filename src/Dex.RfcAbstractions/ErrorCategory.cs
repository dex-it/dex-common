namespace Dex.RfcExceptions;

/// <summary>Категория проблемы. Инфраструктура (middleware) мапит её в HTTP-статус и RFC 9457 type.</summary>
public enum ErrorCategory
{
    Unknown = 0,        // -> 500
    Validation,         // -> 400 (совместимость; НЕ 422)
    BadRequest,         // -> 400
    Unauthorized,       // -> 401
    Forbidden,          // -> 403
    NotFound,           // -> 404
    Conflict,           // -> 409
    AlreadyExists,      // -> 409
    PreconditionFailed, // -> 412
    PaymentRequired,    // -> 402
    TooManyRequests,    // -> 429
    Timeout,            // -> 408
    IntegrationError,   // -> 412
    ServiceUnavailable  // -> 503
}