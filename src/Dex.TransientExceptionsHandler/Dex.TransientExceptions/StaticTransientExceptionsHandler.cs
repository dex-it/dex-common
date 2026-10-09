using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;
using Dex.TransientExceptions.Exceptions;
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
    /// TimeoutException, IOException, SocketException, OperationCanceledException,
    /// отказы Polly 7 и 8 (наследники Polly.ExecutionRejectedException: таймаут, circuit breaker, rate limiter, bulkhead)
    /// HttpCodes: 408, 429, 5XX
    /// </summary>
    /// <remarks>
    /// Отмену изнутри консьюмера MassTransit подменяет на ConsumerCanceledException; как OperationCanceledException
    /// её передаёт политике HandleTransient из Dex.MassTransit.Rabbit (на нём же UseRetryConfiguration и UseRedeliveryRetryConfiguration),
    /// обычный Handle в UseMessageRetry — нет.
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
        typeof(RedisTimeoutException)
    ]).ToFrozenSet();

    // Polly 7 (Polly.dll) и Polly 8 (Polly.Core) объявляют этот тип в разных сборках. Ссылка на любую из них давала бы
    // потребителю с другой версией CS0433 на одноимённых типах, поэтому сравнение по имени, без зависимости от Polly
    private const string PollyExecutionRejectedException = "Polly.ExecutionRejectedException";

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

    private static bool StaticCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (ExceptionsCheckInternal(StaticTransientExceptions, exception, innerExceptionsSearchDepth))
            return true;

        if (PredicateCheckInternal(StaticTransientExceptionsPredicate, exception, innerExceptionsSearchDepth))
            return true;

        if (IsPollyRejection(exception))
            return true;

        foreach (var innerException in EnumerateInnerExceptions(exception, innerExceptionsSearchDepth))
            if (IsPollyRejection(innerException))
                return true;

        return false;
    }

    private static bool IsPollyRejection(Exception exception)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
            if (string.Equals(type.FullName, PollyExecutionRejectedException, StringComparison.Ordinal))
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