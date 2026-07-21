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
                rfcProblem.Type ??= rfcEx.ErrorCode is { } code ? $"/problems/{code}" : typeByCategory;
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
        StatusCodes.Status400BadRequest => RfcTypes.BadRequest,
        StatusCodes.Status401Unauthorized => RfcTypes.Unauthorized,
        StatusCodes.Status402PaymentRequired => RfcTypes.PaymentError,
        StatusCodes.Status403Forbidden => RfcTypes.Forbidden,
        StatusCodes.Status404NotFound => RfcTypes.NotFound,
        StatusCodes.Status405MethodNotAllowed => RfcTypes.MethodNotAllowed,
        StatusCodes.Status406NotAcceptable => RfcTypes.NotAcceptable,
        StatusCodes.Status407ProxyAuthenticationRequired => RfcTypes.ProxyAuthenticationRequired,
        StatusCodes.Status408RequestTimeout => RfcTypes.Timeout,
        StatusCodes.Status409Conflict => RfcTypes.Conflict,
        StatusCodes.Status410Gone => RfcTypes.Gone,
        StatusCodes.Status411LengthRequired => RfcTypes.LengthRequired,
        StatusCodes.Status412PreconditionFailed => RfcTypes.PreconditionFailed,
        StatusCodes.Status413PayloadTooLarge => RfcTypes.PayloadTooLarge,
        StatusCodes.Status414UriTooLong => RfcTypes.UriTooLong,
        StatusCodes.Status415UnsupportedMediaType => RfcTypes.UnsupportedMediaType,
        StatusCodes.Status416RangeNotSatisfiable => RfcTypes.RangeNotSatisfiable,
        StatusCodes.Status417ExpectationFailed => RfcTypes.ExpectationFailed,
        StatusCodes.Status418ImATeapot => "☕🤦‍♂️",
        StatusCodes.Status421MisdirectedRequest => RfcTypes.MisdirectedRequest,
        StatusCodes.Status422UnprocessableEntity => RfcTypes.ValidationError,
        StatusCodes.Status423Locked => RfcTypes.Locked,
        StatusCodes.Status424FailedDependency => RfcTypes.FailedDependency,
        425 => RfcTypes.TooEarly,
        StatusCodes.Status426UpgradeRequired => RfcTypes.UpgradeRequired,
        StatusCodes.Status428PreconditionRequired => RfcTypes.PreconditionRequired,
        StatusCodes.Status429TooManyRequests => RfcTypes.TooManyRequests,
        StatusCodes.Status431RequestHeaderFieldsTooLarge => RfcTypes.RequestHeaderTooLarge,
        StatusCodes.Status451UnavailableForLegalReasons => RfcTypes.UnavailableForLegalReasons,

        // 5xx Server Errors
        StatusCodes.Status500InternalServerError => RfcTypes.InternalServerError,
        StatusCodes.Status501NotImplemented => RfcTypes.NotImplemented,
        StatusCodes.Status502BadGateway => RfcTypes.BadGateway,
        StatusCodes.Status503ServiceUnavailable => RfcTypes.ServiceUnavailable,
        StatusCodes.Status504GatewayTimeout => RfcTypes.GatewayTimeout,
        StatusCodes.Status505HttpVersionNotsupported => RfcTypes.HttpVersionNotSupported,
        StatusCodes.Status506VariantAlsoNegotiates => RfcTypes.VariantAlsoNegotiates,
        StatusCodes.Status507InsufficientStorage => RfcTypes.InsufficientStorage,
        StatusCodes.Status508LoopDetected => RfcTypes.LoopDetected,
        StatusCodes.Status510NotExtended => RfcTypes.NotExtended,
        StatusCodes.Status511NetworkAuthenticationRequired => RfcTypes.NetworkAuthenticationRequired,

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