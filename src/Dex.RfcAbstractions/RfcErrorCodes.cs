namespace Dex.RfcExceptions;

/// <summary>
/// Коды ошибок для IRfcException.ErrorCode — путь после /problems/ (зеркало RfcTypes без префикса).
/// </summary>
public static class RfcErrorCodes
{
    // 4xx
    public const string BadRequest = "bad-request";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not-found";
    public const string MethodNotAllowed = "method-not-allowed";
    public const string NotAcceptable = "not-acceptable";
    public const string ProxyAuthenticationRequired = "proxy-authentication-required";
    public const string RequestTimeout = "request-timeout";
    public const string Conflict = "conflict";
    public const string Gone = "gone";
    public const string LengthRequired = "length-required";
    public const string PreconditionFailed = "precondition-failed";
    public const string PayloadTooLarge = "payload-too-large";
    public const string UriTooLong = "uri-too-long";
    public const string UnsupportedMediaType = "unsupported-media-type";
    public const string RangeNotSatisfiable = "range-not-satisfiable";
    public const string ExpectationFailed = "expectation-failed";
    public const string MisdirectedRequest = "misdirected-request";
    public const string UnprocessableEntity = "unprocessable-entity";
    public const string ValidationError = "validation-error";
    public const string Locked = "locked";
    public const string FailedDependency = "failed-dependency";
    public const string TooEarly = "too-early";
    public const string UpgradeRequired = "upgrade-required";
    public const string PreconditionRequired = "precondition-required";
    public const string TooManyRequests = "too-many-requests";
    public const string RequestHeaderTooLarge = "request-header-fields-too-large";
    public const string UnavailableForLegalReasons = "unavailable-for-legal-reasons";

    // 5xx
    public const string InternalServerError = "internal-server-error";
    public const string NotImplemented = "not-implemented";
    public const string BadGateway = "bad-gateway";
    public const string ServiceUnavailable = "service-unavailable";
    public const string GatewayTimeout = "gateway-timeout";
    public const string HttpVersionNotSupported = "http-version-not-supported";
    public const string VariantAlsoNegotiates = "variant-also-negotiates";
    public const string InsufficientStorage = "insufficient-storage";
    public const string LoopDetected = "loop-detected";
    public const string NotExtended = "not-extended";
    public const string NetworkAuthenticationRequired = "network-authentication-required";

    // Domain / business
    public const string Blocked = "blocked";
    public const string BusinessRuleViolation = "business-rule-violation";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string Duplicate = "duplicate";
    public const string InvalidState = "invalid-state";
    public const string NotSupported = "not-supported";
    public const string InactiveResource = "inactive-resource";
    public const string PaymentError = "payment-error";
    public const string IntegrationError = "integration-error";
    public const string DependencyFailure = "dependency-failure";
    public const string Timeout = "timeout";
    public const string UserError = "user-error";
    public const string TemporaryNotAvailable = "service-unavailable-temporary";
    public const string AlreadyExist = "conflict/already-exist";
    public const string AlreadyUsed = "conflict/already-used";
}