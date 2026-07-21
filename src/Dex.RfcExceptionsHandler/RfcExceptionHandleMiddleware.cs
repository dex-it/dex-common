using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Dex.Extensions;
using Dex.RfcExceptions;
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
internal sealed class RfcExceptionHandleMiddleware(
    IRfcExceptionHandleConfig config,
    ILogger<RfcExceptionHandleMiddleware> logger,
    IWebHostEnvironment environment) : IMiddleware
{
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

            // rfc code + type
            if (exception is IRfcException rfcEx)
            {
                var (status, typeByCategory) = RfcExceptionCategoryMap.Resolve(rfcEx.Category);
                rfcProblem.Status ??= status;
                rfcProblem.Type ??= rfcEx.ErrorCode is { } code ? RfcErrorCodes.ProblemTypePrefix + code : typeByCategory;
            }
            else
            {
                rfcProblem.Status ??= config.ResolveHttpStatusCode(exception);
                rfcProblem.Type ??= ResolveRfcTypeByHttpStatusCode(rfcProblem.Status.Value);
            }

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

    private static string ResolveRfcTypeByHttpStatusCode(int statusCode) => statusCode switch
    {
        // 4xx Client Errors
        StatusCodes.Status400BadRequest => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Unauthorized,
        StatusCodes.Status402PaymentRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PaymentError,
        StatusCodes.Status403Forbidden => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotFound,
        StatusCodes.Status405MethodNotAllowed => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.MethodNotAllowed,
        StatusCodes.Status406NotAcceptable => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotAcceptable,
        StatusCodes.Status407ProxyAuthenticationRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ProxyAuthenticationRequired,
        StatusCodes.Status408RequestTimeout => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Timeout,
        StatusCodes.Status409Conflict => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Conflict,
        StatusCodes.Status410Gone => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Gone,
        StatusCodes.Status411LengthRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.LengthRequired,
        StatusCodes.Status412PreconditionFailed => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PreconditionFailed,
        StatusCodes.Status413PayloadTooLarge => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PayloadTooLarge,
        StatusCodes.Status414UriTooLong => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.UriTooLong,
        StatusCodes.Status415UnsupportedMediaType => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.UnsupportedMediaType,
        StatusCodes.Status416RangeNotSatisfiable => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.RangeNotSatisfiable,
        StatusCodes.Status417ExpectationFailed => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ExpectationFailed,
        StatusCodes.Status418ImATeapot => "☕🤦‍♂️",
        StatusCodes.Status421MisdirectedRequest => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.MisdirectedRequest,
        StatusCodes.Status422UnprocessableEntity => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ValidationError,
        StatusCodes.Status423Locked => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Locked,
        StatusCodes.Status424FailedDependency => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.FailedDependency,
        425 => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.TooEarly,
        StatusCodes.Status426UpgradeRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.UpgradeRequired,
        StatusCodes.Status428PreconditionRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PreconditionRequired,
        StatusCodes.Status429TooManyRequests => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.TooManyRequests,
        StatusCodes.Status431RequestHeaderFieldsTooLarge => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.RequestHeaderTooLarge,
        StatusCodes.Status451UnavailableForLegalReasons => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.UnavailableForLegalReasons,

        // 5xx Server Errors
        StatusCodes.Status500InternalServerError => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.InternalServerError,
        StatusCodes.Status501NotImplemented => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotImplemented,
        StatusCodes.Status502BadGateway => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.BadGateway,
        StatusCodes.Status503ServiceUnavailable => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ServiceUnavailable,
        StatusCodes.Status504GatewayTimeout => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.GatewayTimeout,
        StatusCodes.Status505HttpVersionNotsupported => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.HttpVersionNotSupported,
        StatusCodes.Status506VariantAlsoNegotiates => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.VariantAlsoNegotiates,
        StatusCodes.Status507InsufficientStorage => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.InsufficientStorage,
        StatusCodes.Status508LoopDetected => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.LoopDetected,
        StatusCodes.Status510NotExtended => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotExtended,
        StatusCodes.Status511NetworkAuthenticationRequired => RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NetworkAuthenticationRequired,

        _ => "unknown"
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

        // custom rfc extensions
        if (rfcException.Extensions is { Count: > 0 } exExt)
            foreach (var kv in exExt)
                extensions[kv.Key] = kv.Value;

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