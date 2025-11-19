using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;
using Dex.TransientExceptions.Exceptions;
using Dex.TransientExceptions.Helpers;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Refit;
using StackExchange.Redis;

namespace Dex.TransientExceptions;

public partial class TransientExceptionsHandler
{
    private const int DefaultInnerExceptionSearchDepth = 10;

    /// <summary>
    /// Стандартная конфигурация, включает наиболее распространенные временные ошибки
    /// TimeoutException, IOException, SocketException, OperationCanceledException
    /// HttpCodes: 408, 429, 5XX
    /// </summary>
    public static TransientExceptionsHandler Default { get; } = new(runBuild: true);

    /// <summary>
    /// Все ошибки будут считаться трансиентными
    /// </summary>
    public static TransientExceptionsHandler RetryAll { get; } = new([typeof(Exception)], runBuild: true);

    private static readonly FrozenSet<Type> StaticTransientExceptions = ((Type[])
    [
        typeof(TimeoutException),
        typeof(IOException),
        typeof(SocketException),
        typeof(TransientException),
        typeof(OutOfMemoryException),
        typeof(DbUpdateConcurrencyException),
        typeof(OperationCanceledException),
        typeof(RedisConnectionException),
        typeof(RedisTimeoutException)
    ]).ToFrozenSet();

    private static readonly FrozenDictionary<Type, Func<Exception, bool>> StaticTransientExceptionsPredicate = new Dictionary<Type, Func<Exception, bool>>
    {
        [typeof(NpgsqlException)] = exception => exception is NpgsqlException {IsTransient: true},
        [typeof(HttpRequestException)] = exception => exception is HttpRequestException
        {
            StatusCode:
            HttpStatusCode.RequestTimeout or // 408 Request Timeout
            HttpStatusCode.TooManyRequests or // 429 Too Many Requests
            >= HttpStatusCode.InternalServerError // 5xx All server-side errors
        },
        [typeof(ApiException)] = exception => exception is ApiException
        {
            StatusCode:
            HttpStatusCode.RequestTimeout or // 408 Request Timeout
            HttpStatusCode.TooManyRequests or // 429 Too Many Requests
            >= HttpStatusCode.InternalServerError // 5xx All server-side errors
        },
        [typeof(RpcException)] = exception => exception is RpcException
        {
            StatusCode:
            StatusCode.Unknown or
            StatusCode.Internal or
            StatusCode.Unavailable or
            StatusCode.Aborted or
            StatusCode.DeadlineExceeded or
            StatusCode.ResourceExhausted
        },
        [typeof(WebException)] = exception => exception is WebException
        {
            Status:
            WebExceptionStatus.ConnectFailure or
            WebExceptionStatus.Timeout or
            WebExceptionStatus.NameResolutionFailure or
            WebExceptionStatus.ProxyNameResolutionFailure or
            WebExceptionStatus.SendFailure or
            WebExceptionStatus.ReceiveFailure or
            WebExceptionStatus.KeepAliveFailure or
            WebExceptionStatus.PipelineFailure or
            WebExceptionStatus.ProtocolError or
            WebExceptionStatus.Pending
        }
    }.ToFrozenDictionary();

    private static bool TransientExceptionInterfaceCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        if (exception is ITransientException)
            return true;

        if (exception is ITransientExceptionCandidate {IsTransient: true})
            return true;

        if (exception.GetInnerExceptions(innerExceptionsSearchDepth).Any(x => x is ITransientException))
            return true;

        if (exception.GetInnerExceptions(innerExceptionsSearchDepth).Any(x => x is ITransientExceptionCandidate {IsTransient: true}))
            return true;

        return false;
    }

    private static bool StaticCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (ExceptionsCheckInternal(StaticTransientExceptions, exception, innerExceptionsSearchDepth))
            return true;

        if (PredicateCheckInternal(StaticTransientExceptionsPredicate, exception, innerExceptionsSearchDepth))
            return true;

        return false;
    }

    private static bool ExceptionsCheckInternal(FrozenSet<Type> exceptions, Exception exception, int innerExceptionsSearchDepth)
    {
        if (exceptions.Count <= 0)
            return false;

        // main exception check
        if (exceptions.Contains(exception.GetType()) || exceptions.Any(x => x.IsInstanceOfType(exception)))
            return true;

        // inner exceptions check
        foreach (var innerException in exception.GetInnerExceptions(innerExceptionsSearchDepth))
            if (exceptions.Contains(innerException.GetType()) || exceptions.Any(x => x.IsInstanceOfType(innerException)))
                return true;

        return false;
    }

    private static bool PredicateCheckInternal(FrozenDictionary<Type, Func<Exception, bool>> exceptions, Exception exception, int innerExceptionsSearchDepth)
    {
        if (exceptions.Count <= 0)
            return false;

        // main exception check
        if (exceptions.TryGetValue(exception.GetType(), out var predicate))
            if (predicate(exception))
                return true;

        // inner exceptions check
        foreach (var innerException in exception.GetInnerExceptions(innerExceptionsSearchDepth))
            if (exceptions.TryGetValue(innerException.GetType(), out var predicateForInner))
                if (predicateForInner(innerException))
                    return true;

        return false;
    }

    public static implicit operator Func<Exception, bool>(TransientExceptionsHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return handler.ToFunc();
    }

    public Func<Exception, bool> ToFunc() => Check;
}