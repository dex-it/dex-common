namespace Dex.RfcAbstractions;

/// <summary>
/// Категория проблемы. Инфраструктура (middleware) мапит её в HTTP-статус и RFC 9457 type.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Непредвиденная ошибка. HTTP 500, код <see cref="RfcErrorCodes.InternalServerError"/>.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Ошибка валидации входных данных. HTTP 400 (для совместимости, НЕ 422), код <see cref="RfcErrorCodes.ValidationError"/>.
    /// </summary>
    Validation,

    /// <summary>
    /// Некорректный запрос. HTTP 400, код <see cref="RfcErrorCodes.BadRequest"/>.
    /// </summary>
    BadRequest,

    /// <summary>
    /// Ошибка на стороне пользователя (бизнес-правило). HTTP 400, код <see cref="RfcErrorCodes.UserError"/>.
    /// </summary>
    UserError,

    /// <summary>
    /// Не аутентифицирован. HTTP 401, код <see cref="RfcErrorCodes.Unauthorized"/>.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Доступ запрещён. HTTP 403, код <see cref="RfcErrorCodes.Forbidden"/>.
    /// </summary>
    Forbidden,

    /// <summary>
    /// Ресурс не найден. HTTP 404, код <see cref="RfcErrorCodes.NotFound"/>.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт состояния. HTTP 409, код <see cref="RfcErrorCodes.Conflict"/>.
    /// </summary>
    Conflict,

    /// <summary>
    /// Ресурс уже существует. HTTP 409, код <see cref="RfcErrorCodes.AlreadyExist"/>.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// Не выполнены предусловия. HTTP 412, код <see cref="RfcErrorCodes.PreconditionFailed"/>.
    /// </summary>
    PreconditionFailed,

    /// <summary>
    /// Требуется оплата. HTTP 402, код <see cref="RfcErrorCodes.PaymentError"/>.
    /// </summary>
    PaymentRequired,

    /// <summary>
    /// Превышен лимит запросов. HTTP 429, код <see cref="RfcErrorCodes.TooManyRequests"/>.
    /// </summary>
    TooManyRequests,

    /// <summary>
    /// Тайм-аут запроса. HTTP 408, код <see cref="RfcErrorCodes.Timeout"/>.
    /// </summary>
    Timeout,

    /// <summary>
    /// Ошибка интеграции с внешним сервисом (серверная, retriable). HTTP 503, код <see cref="RfcErrorCodes.IntegrationError"/>.
    /// </summary>
    IntegrationError,

    /// <summary>
    /// Сервис недоступен. HTTP 503, код <see cref="RfcErrorCodes.ServiceUnavailable"/>.
    /// </summary>
    ServiceUnavailable
}