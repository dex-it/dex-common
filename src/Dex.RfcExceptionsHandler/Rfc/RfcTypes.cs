namespace Dex.RfcExceptionsHandler.Rfc;

/// <summary>
/// RFC 7807 problem type identifiers.
/// </summary>
public static class RfcTypes
{
    // 4xx
    public const string BadRequest = "/problems/bad-request";
    public const string Unauthorized = "/problems/unauthorized";
    public const string Forbidden = "/problems/forbidden";
    public const string NotFound = "/problems/not-found";
    public const string MethodNotAllowed = "/problems/method-not-allowed";
    public const string NotAcceptable = "/problems/not-acceptable";
    public const string ProxyAuthenticationRequired = "/problems/proxy-authentication-required";
    public const string RequestTimeout = "/problems/request-timeout";
    public const string Conflict = "/problems/conflict";
    public const string Gone = "/problems/gone";
    public const string LengthRequired = "/problems/length-required";
    public const string PreconditionFailed = "/problems/precondition-failed";
    public const string PayloadTooLarge = "/problems/payload-too-large";
    public const string UriTooLong = "/problems/uri-too-long";
    public const string UnsupportedMediaType = "/problems/unsupported-media-type";
    public const string RangeNotSatisfiable = "/problems/range-not-satisfiable";
    public const string ExpectationFailed = "/problems/expectation-failed";
    public const string MisdirectedRequest = "/problems/misdirected-request";
    public const string UnprocessableEntity = "/problems/unprocessable-entity";
    public const string ValidationError = "/problems/validation-error";
    public const string Locked = "/problems/locked";
    public const string FailedDependency = "/problems/failed-dependency";
    public const string TooEarly = "/problems/too-early";
    public const string UpgradeRequired = "/problems/upgrade-required";
    public const string PreconditionRequired = "/problems/precondition-required";
    public const string TooManyRequests = "/problems/too-many-requests";
    public const string RequestHeaderTooLarge = "/problems/request-header-fields-too-large";
    public const string UnavailableForLegalReasons = "/problems/unavailable-for-legal-reasons";

    // 5xx
    public const string InternalServerError = "/problems/internal-server-error";
    public const string NotImplemented = "/problems/not-implemented";
    public const string BadGateway = "/problems/bad-gateway";
    public const string ServiceUnavailable = "/problems/service-unavailable";
    public const string GatewayTimeout = "/problems/gateway-timeout";
    public const string HttpVersionNotSupported = "/problems/http-version-not-supported";
    public const string VariantAlsoNegotiates = "/problems/variant-also-negotiates";
    public const string InsufficientStorage = "/problems/insufficient-storage";
    public const string LoopDetected = "/problems/loop-detected";
    public const string NotExtended = "/problems/not-extended";
    public const string NetworkAuthenticationRequired = "/problems/network-authentication-required";

    // Domain / business
    public const string Blocked = "/problems/blocked";
    public const string BusinessRuleViolation = "/problems/business-rule-violation";
    public const string ConcurrencyConflict = "/problems/concurrency-conflict";
    public const string Duplicate = "/problems/duplicate";
    public const string InvalidState = "/problems/invalid-state";
    public const string NotSupported = "/problems/not-supported";
    public const string InactiveResource = "/problems/inactive-resource";
    public const string PaymentError = "/problems/payment-error";
    public const string IntegrationError = "/problems/integration-error";
    public const string DependencyFailure = "/problems/dependency-failure";
    public const string Timeout = "/problems/timeout";
    public const string UserError = "/problems/user-error";
    public const string TemporaryNotAvailable = "/problems/service-unavailable-temporary";
    public const string AlreadyExist = "/problems/conflict/already-exist";
    public const string AlreadyUsed = "/problems/conflict/already-used";
}