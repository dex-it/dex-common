using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;
using Dex.TransientExceptions.Exceptions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;
using Refit;
using StackExchange.Redis;

namespace Dex.TransientExceptions;

public partial class TransientExceptionsHandler
{
    private const int DefaultInnerExceptionSearchDepth = 10;

    /// <summary>
    /// Стандартная конфигурация, включает наиболее распространенные временные ошибки
    /// TimeoutException, IOException, SocketException, OperationCanceledException,
    /// отказы Polly 8 (ExecutionRejectedException: таймаут, circuit breaker, rate limiter, bulkhead)
    /// HttpCodes: 408, 429, 5XX
    /// </summary>
    /// <remarks>
    /// Отмену изнутри консьюмера MassTransit подменяет на ConsumerCanceledException; как OperationCanceledException
    /// её передают политике UseRetryConfiguration и UseRedeliveryRetryConfiguration из Dex.MassTransit.Rabbit.
    /// </remarks>
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
        typeof(RedisTimeoutException),
        typeof(ExecutionRejectedException)
    ]).ToFrozenSet();

    private static readonly FrozenDictionary<Type, Func<Exception, bool>> StaticTransientExceptionsPredicate = new Dictionary<Type, Func<Exception, bool>>
    {
        [typeof(NpgsqlException)] = exception => exception is NpgsqlException { IsTransient: true },
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

    // null = кандидат не найден, продолжать проверки
    // true/false = кандидат найден, его решение финальное
    private static bool? TransientExceptionInterfaceCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        // ITransientExceptionCandidate проверяется первым — более специфичный контракт,
        // позволяет явно переопределить поведение даже если базовый класс реализует ITransientException
        if (exception is ITransientExceptionCandidate candidate)
            return candidate.IsTransient;

        if (exception is ITransientException)
            return true;

        foreach (var inner in EnumerateInnerExceptions(exception, innerExceptionsSearchDepth))
        {
            if (inner is ITransientExceptionCandidate innerCandidate)
                return innerCandidate.IsTransient;

            if (inner is ITransientException)
                return true;
        }

        return null;
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
        foreach (var innerException in EnumerateInnerExceptions(exception, innerExceptionsSearchDepth))
            if (exceptions.Contains(innerException.GetType()) || exceptions.Any(x => x.IsInstanceOfType(innerException)))
                return true;

        return false;
    }

    private static bool PredicateCheckInternal(FrozenDictionary<Type, Func<Exception, bool>> exceptions, Exception exception, int innerExceptionsSearchDepth)
    {
        if (exceptions.Count <= 0)
            return false;

        // main exception check
        if (AnyPredicateMatches(exceptions, exception))
            return true;

        // inner exceptions check
        foreach (var innerException in EnumerateInnerExceptions(exception, innerExceptionsSearchDepth))
            if (AnyPredicateMatches(exceptions, innerException))
                return true;

        return false;
    }

    // предикат базового типа применяется и к наследникам, как Add(Type): иначе PostgresException мимо предиката NpgsqlException
    private static bool AnyPredicateMatches(FrozenDictionary<Type, Func<Exception, bool>> exceptions, Exception exception)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
            if (exceptions.TryGetValue(type, out var predicate) && predicate(exception))
                return true;

        return false;
    }

    // Вложенные исключения без самого исключения, в глубину; у AggregateException — все InnerExceptions, а не только первое.
    // Само исключение — первый уровень глубины: при depth = N проверяются уровни вложенности 1..N-1.
    private static IEnumerable<Exception> EnumerateInnerExceptions(Exception exception, int depth)
    {
        var pending = new Stack<(Exception Exception, int Level)>();
        PushInner(pending, exception, level: 1, depth);

        while (pending.Count > 0)
        {
            var (current, level) = pending.Pop();
            yield return current;

            PushInner(pending, current, level + 1, depth);
        }
    }

    private static void PushInner(Stack<(Exception Exception, int Level)> pending, Exception exception, int level, int depth)
    {
        if (level >= depth)
            return;

        if (exception is AggregateException aggregate)
        {
            // в обратном порядке: со стека первым уйдёт первое вложенное
            for (var i = aggregate.InnerExceptions.Count - 1; i >= 0; i--)
                pending.Push((aggregate.InnerExceptions[i], level));
        }
        else if (exception.InnerException is not null)
        {
            pending.Push((exception.InnerException, level));
        }
    }

    public static implicit operator Func<Exception, bool>(TransientExceptionsHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return handler.ToFunc();
    }

    public Func<Exception, bool> ToFunc() => Check;
}