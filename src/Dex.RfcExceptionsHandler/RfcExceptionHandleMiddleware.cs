using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dex.Extensions;
using Dex.RfcAbstractions;
using Dex.RfcExceptionsHandler.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.RfcExceptionsHandler;

/// <summary>
/// Перехватчик исключений, возникающих при обработке запросов к API.
/// </summary>
internal sealed partial class RfcExceptionHandleMiddleware(
    IRfcExceptionHandleConfig config,
    ILogger<RfcExceptionHandleMiddleware> logger,
    IWebHostEnvironment environment) : IMiddleware
{
    // допустимый формат ErrorCode: lowercase-kebab-сегменты через '/'
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*(/[a-z0-9]+(-[a-z0-9]+)*)*$")]
    private static partial Regex ErrorCodeFormatRegex();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // перехват ошибок лучше не отменять
        var cToken = CancellationToken.None;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            exception = config.Map(exception);

            // rfc problem
            var rfcProblem = ToProblemDetails(exception);

            // rfc code (без префикса) — префикс клеится в одной точке ниже
            string errorCode;
            if (exception is IRfcException rfcEx)
            {
                var (status, codeByCategory) = RfcExceptionCategoryMap.Resolve(rfcEx.Category);
                rfcProblem.Status ??= status;
                // нормализуем доменный код ДО выбора; если после нормализации пусто
                // (null/whitespace/"/problems/"/"///") — берём код по категории
                var normalized = NormalizeErrorCode(rfcEx.ErrorCode);
                errorCode = normalized.Length == 0 ? codeByCategory : normalized;
            }
            else
            {
                rfcProblem.Status ??= config.ResolveHttpStatusCode(exception);
                errorCode = ResolveErrorCodeByHttpStatusCode(rfcProblem.Status.Value);
            }

            // rfc type — единственная точка склейки префикса
            rfcProblem.Type ??= RfcTypeConstants.ProblemTypePrefix + errorCode;

            // rfc stackTrace
            if (string.IsNullOrEmpty(exception.StackTrace) is false && environment.IsProduction() is false)
                rfcProblem.Extensions[RfcExtensionKeys.StackTrace] = string.Join(Environment.NewLine, exception.GetInnerExceptions().Select(x => x.StackTrace));

            // rfc traceId
            rfcProblem.Extensions[RfcExtensionKeys.TraceId] = Activity.Current?.Id ?? context.TraceIdentifier;

            // rfc Instance
            rfcProblem.Instance = context.Request.GetEncodedPathAndQuery();

            var responseJson = JsonSerializer.Serialize(rfcProblem, config.JsonSerializerOptions);
            if (rfcProblem.Status >= StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Request failed");
            else
                logger.LogWarning(exception, "Request complete with warning. {MiddlewareResponse}", responseJson);

            try
            {
                context.Response.StatusCode = rfcProblem.Status.Value;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsync(responseJson, cancellationToken: cToken);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (InvalidOperationException e) when (e.Message.Contains("response has already started", StringComparison.CurrentCultureIgnoreCase))
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// Нормализует и валидирует доменный код перед склейкой с префиксом: снимает уже
    /// присутствующий префикс /problems/ и ведущие слэши. Возвращает пустую строку для
    /// null/whitespace, вырожденных значений ("/problems/", "///") и значений, не подходящих
    /// под формат lowercase-kebab (пробелы, "..", регистр, спецсимволы) — чтобы вызывающий
    /// код откатился на код категории и не получил битый или невалидный как URI type.
    /// </summary>
    private static string NormalizeErrorCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return string.Empty;

        var code = errorCode.AsSpan().Trim();

        // снять уже присутствующий префикс /problems/ (миграционная ловушка из 8.0.1)
        if (code.StartsWith(RfcTypeConstants.ProblemTypePrefix, StringComparison.OrdinalIgnoreCase))
            code = code[RfcTypeConstants.ProblemTypePrefix.Length..];

        // снять ведущие слэши (в т.ч. случай "/problems///" -> "//" -> "")
        code = code.TrimStart('/');

        if (code.IsEmpty)
            return string.Empty;

        // отбросить всё, что не lowercase-kebab-сегменты (пробелы, "..", регистр и т.п.)
        var result = code.ToString();
        return ErrorCodeFormatRegex().IsMatch(result) ? result : string.Empty;
    }

    private static string ResolveErrorCodeByHttpStatusCode(int statusCode) => statusCode switch
    {
        // 4xx Client Errors
        StatusCodes.Status400BadRequest => RfcErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => RfcErrorCodes.Unauthorized,
        StatusCodes.Status402PaymentRequired => RfcErrorCodes.PaymentError,
        StatusCodes.Status403Forbidden => RfcErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => RfcErrorCodes.NotFound,
        StatusCodes.Status405MethodNotAllowed => RfcErrorCodes.MethodNotAllowed,
        StatusCodes.Status406NotAcceptable => RfcErrorCodes.NotAcceptable,
        StatusCodes.Status407ProxyAuthenticationRequired => RfcErrorCodes.ProxyAuthenticationRequired,
        StatusCodes.Status408RequestTimeout => RfcErrorCodes.Timeout,
        StatusCodes.Status409Conflict => RfcErrorCodes.Conflict,
        StatusCodes.Status410Gone => RfcErrorCodes.Gone,
        StatusCodes.Status411LengthRequired => RfcErrorCodes.LengthRequired,
        StatusCodes.Status412PreconditionFailed => RfcErrorCodes.PreconditionFailed,
        StatusCodes.Status413PayloadTooLarge => RfcErrorCodes.PayloadTooLarge,
        StatusCodes.Status414UriTooLong => RfcErrorCodes.UriTooLong,
        StatusCodes.Status415UnsupportedMediaType => RfcErrorCodes.UnsupportedMediaType,
        StatusCodes.Status416RangeNotSatisfiable => RfcErrorCodes.RangeNotSatisfiable,
        StatusCodes.Status417ExpectationFailed => RfcErrorCodes.ExpectationFailed,
        StatusCodes.Status418ImATeapot => RfcErrorCodes.ImATeapot,
        StatusCodes.Status421MisdirectedRequest => RfcErrorCodes.MisdirectedRequest,
        StatusCodes.Status422UnprocessableEntity => RfcErrorCodes.ValidationError,
        StatusCodes.Status423Locked => RfcErrorCodes.Locked,
        StatusCodes.Status424FailedDependency => RfcErrorCodes.FailedDependency,
        425 => RfcErrorCodes.TooEarly,
        StatusCodes.Status426UpgradeRequired => RfcErrorCodes.UpgradeRequired,
        StatusCodes.Status428PreconditionRequired => RfcErrorCodes.PreconditionRequired,
        StatusCodes.Status429TooManyRequests => RfcErrorCodes.TooManyRequests,
        StatusCodes.Status431RequestHeaderFieldsTooLarge => RfcErrorCodes.RequestHeaderTooLarge,
        StatusCodes.Status451UnavailableForLegalReasons => RfcErrorCodes.UnavailableForLegalReasons,

        // 5xx Server Errors
        StatusCodes.Status500InternalServerError => RfcErrorCodes.InternalServerError,
        StatusCodes.Status501NotImplemented => RfcErrorCodes.NotImplemented,
        StatusCodes.Status502BadGateway => RfcErrorCodes.BadGateway,
        StatusCodes.Status503ServiceUnavailable => RfcErrorCodes.ServiceUnavailable,
        StatusCodes.Status504GatewayTimeout => RfcErrorCodes.GatewayTimeout,
        StatusCodes.Status505HttpVersionNotsupported => RfcErrorCodes.HttpVersionNotSupported,
        StatusCodes.Status506VariantAlsoNegotiates => RfcErrorCodes.VariantAlsoNegotiates,
        StatusCodes.Status507InsufficientStorage => RfcErrorCodes.InsufficientStorage,
        StatusCodes.Status508LoopDetected => RfcErrorCodes.LoopDetected,
        StatusCodes.Status510NotExtended => RfcErrorCodes.NotExtended,
        StatusCodes.Status511NetworkAuthenticationRequired => RfcErrorCodes.NetworkAuthenticationRequired,

        _ => RfcErrorCodes.Unknown
    };

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Инстансы, реализующие интерфейс, будут в конкретных проектах")]
    private ProblemDetails ToProblemDetails<T>(T exception) where T : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

        var extensions = new Dictionary<string, object?>(RfcExtensionKeys.DefaultExtensionsCapacity)
        {
            [RfcExtensionKeys.ExceptionType] = exception.GetType().Name.Replace("`1", string.Empty, StringComparison.Ordinal)
        };

        if (exception.Data.Count > 0)
            extensions[RfcExtensionKeys.ExceptionData] = exception.Data;

        // generic вариант, для ошибок, не реализующих IRfcException
        if (exception is not IRfcException rfcException)
            return new ProblemDetails
            {
                Title = "Unexpected error occurred",
                Detail = environment.IsProduction() ? $"Details not available for non-rfc exception typeof {typeof(T).Name}" : exception.Message,
                Extensions = extensions
            };

        // custom rfc extensions — reserved-ключи (RFC 9457 top-level + служебные) не
        // перезаписываем: иначе дубль-ключи в JSON и подмена валидированного type/status
        if (rfcException.Extensions is { Count: > 0 } exExt)
            foreach (var kv in exExt)
            {
                if (RfcExtensionKeys.ReservedKeys.Contains(kv.Key))
                {
                    logger.LogDebug("Custom extension key {ExtensionKey} is reserved and was skipped", kv.Key);
                    continue;
                }

                extensions[kv.Key] = kv.Value;
            }

        var problemDetails = new ProblemDetails
        {
            Title = rfcException.Title,
            Detail = rfcException.Detail,
            Extensions = extensions
            // Type/Status выставляются выше по Category/ErrorCode
        };

        return problemDetails;
    }
}